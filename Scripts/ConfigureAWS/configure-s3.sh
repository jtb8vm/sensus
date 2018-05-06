#!/bin/sh

if [ $# -ne 3 ]; then
    echo "Usage:  ./configure-s3.sh [region] [root id]"
    echo "\t[name]:  Informative name for bucket (alphanumerics and dashes)"
    echo "\t[region]:  AWS region to use (e.g., us-east-1)"
    echo "\t[root id]:  Account ID that will own the data (12 digits, no dashes)"
    exit 1
fi

# create random bucket in given region
echo "Creating S3 bucket..."
bucket="$1-$(uuidgen | tr '[:upper:]' '[:lower:]')"
aws s3api create-bucket --bucket $bucket --region $2
if [ $? -ne 0 ]; then
    echo "Failed to create bucket."
    exit $?
fi

# enable versioning on the bucket for safety purposes
aws s3api put-bucket-versioning --bucket $bucket --versioning-configuration Status=Enabled
if [ $? -ne 0 ]; then
    echo "Failed to enable bucket versioning."
    exit $?
fi

# create IAM user
echo "Creating IAM user..."
iamUserName="${bucket}"
iamUserARN=$(aws iam create-user --user-name $iamUserName | jq -r .User.Arn)
if [ $? -ne 0 ]; then
    echo "Failed to create IAM user."
    exit $?
fi

# attach read-only policy for bucket to IAM user
cp ./iam-policy.json tmp.json
sed -i "" "s/bucketName/$bucket/" ./tmp.json
aws iam put-user-policy --user-name $iamUserName --policy-name $iamUserName --policy-document file://tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to put IAM user policy."
    exit $?
fi
rm tmp.json

# give the user a bit to propagate, then attach bucket policy giving access to the root user and IAM user.
sleep 15
cp ./bucket-policy.json tmp.json
sed -i "" "s/bucketId/$bucket/" ./tmp.json
sed -i "" "s/rootAccountId/$3/" ./tmp.json
sed -i "" "s#iamUserARN#$iamUserARN#" ./tmp.json
aws s3api put-bucket-policy --bucket $bucket --policy file://./tmp.json
if [ $? -ne 0 ]; then
    rm tmp.json
    echo "Failed to attach bucket policy."
    exit $?
fi
rm tmp.json

echo "All done. Bucket:  $bucket"
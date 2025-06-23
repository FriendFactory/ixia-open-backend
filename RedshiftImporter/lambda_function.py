import sys

sys.path.insert(0, 'package')

import redshift_connector
import boto3
import json
import os


s3 = boto3.resource('s3')


def lambda_handler(event, context):
    print('Connecting to Redshift')
    print(os.environ['HOST'])
    print(os.environ['DATABASE'])

    try:

        conn = redshift_connector.connect(
            host=os.environ['HOST'],
            database=os.environ['DATABASE'],
            user=os.environ['USER'],
            password=os.environ['PASSWORD']
        )

        cursor: redshift_connector.Cursor = conn.cursor()

        for item in event['Records']:
            bucket = item['s3']['bucket']['name']
            key = item['s3']['object']['key']

            print(f'bucket={bucket} key={key}')

            command = "copy input.amplitude from '" + f's3://{bucket}/{key.replace("%23", "#")}' + "' credentials 'aws_iam_role=arn:aws:iam::722913253728:role/service-role/AmazonRedshift-CommandsAccessRole-20220128T114412' gzip format as json 'auto'"
            cursor.execute(command)
            conn.commit()
            print(command)
    except:
        print('Error occurred')
        for item in event['Records']:
            bucket = item['s3']['bucket']['name']
            key = item['s3']['object']['key']
            copy_source = {
                'Bucket': bucket,
                'Key': key
                }
            bucket = s3.Bucket(bucket)
            bucket.copy(copy_source, f'error/{key}.error')


    return {
        'statusCode': 200,
        'body': json.dumps('Hello from Lambda!')
    }

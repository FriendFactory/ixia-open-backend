resource "aws_sqs_queue" "video-conversion-job-creation" {
  name                      = "${var.env}-video-conversion-job-creation"
  max_message_size          = 262144
  receive_wait_time_seconds = 20
}

resource "aws_lambda_function" "video-conversion-job-creator" {
  function_name = "${var.env}-video-conversion-job-creator"

  runtime                        = "dotnetcore3.1"
  handler                        = "VideoServer.CreateConversionJobLambda::VideoServer.CreateConversionJobLambda.Function::Handler"
  role                           = aws_iam_role.video-conversion-lambda-exec.arn
  reserved_concurrent_executions = 1
}

resource "aws_lambda_event_source_mapping" "video-conversion-event-mapping" {
  event_source_arn = aws_sqs_queue.video-conversion-job-creation.arn
  enabled          = true
  function_name    = aws_lambda_function.video-conversion-job-creator.arn
  batch_size       = 1
}

resource "aws_iam_role" "video-conversion-lambda-exec" {
  name = "${var.env}-video-conversion-job-creator-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Sid    = ""
      Principal = {
        Service = "lambda.amazonaws.com"
      }
      }
    ]
  })

  inline_policy {
    name = "${var.env}-video-conversion-job-creator-role-policy"

    policy = jsonencode({
      Version = "2012-10-17"
      Statement = [
        {
          Action   = ["logs:*", "mediaconvert:*", "s3:*", "s3-object-lambda:*", "sqs:*"]
          Effect   = "Allow"
          Resource = "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "iam:PassRole"
            ],
            "Resource": "*",
            "Condition": {
                "StringLike": {
                    "iam:PassedToService": [
                        "mediaconvert.amazonaws.com"
                    ]
                }
            }
        }
      ]
    })
  }
}

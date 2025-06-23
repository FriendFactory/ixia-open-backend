resource "aws_sqs_queue" "video_conversion_job_creation" {
  name                      = "${var.env}-video-conversion-job-creation"
  max_message_size          = 262144
  receive_wait_time_seconds = 20
}

resource "aws_lambda_function" "video_conversion_job_creator" {
  function_name = "${var.env}-video-conversion-job-creator"

  filename = "dummy.zip"
  runtime  = "dotnetcore3.1"
  handler  = "VideoServer.CreateConversionJobLambda::VideoServer.CreateConversionJobLambda.Function::Handler"
  role     = aws_iam_role.video_conversion_lambda_exec.arn

  memory_size                    = 512
  timeout                        = 15
  reserved_concurrent_executions = 1
}

resource "aws_lambda_event_source_mapping" "video_conversion_event_mapping" {
  event_source_arn = aws_sqs_queue.video_conversion_job_creation.arn
  enabled          = true
  function_name    = aws_lambda_function.video_conversion_job_creator.arn
  batch_size       = 1
}

resource "aws_iam_role" "video_conversion_lambda_exec" {
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
          "Effect" : "Allow",
          "Action" : [
            "iam:PassRole"
          ],
          "Resource" : "*",
          "Condition" : {
            "StringLike" : {
              "iam:PassedToService" : [
                "mediaconvert.amazonaws.com"
              ]
            }
          }
        }
      ]
    })
  }
}

resource "aws_sqs_queue" "video_conversion_job_completed" {
  name                      = "${var.env}-video-conversion-job-completion"
  max_message_size          = 262144
  receive_wait_time_seconds = 20
}

resource "aws_media_convert_queue" "media_converter_queue" {
  name   = var.env
  status = "ACTIVE"
}

resource "aws_cloudwatch_event_rule" "video_conversion_completed" {
  name        = "${var.env}-video-converted"
  description = "Video from queue ${var.env} has been converted"

  event_pattern = <<EOF
        {
            "source": ["aws.mediaconvert"],
            "detail-type": ["MediaConvert Job State Change"],
            "detail": {
                "status": ["COMPLETE"],
                "queue": ["${aws_media_convert_queue.media_converter_queue.arn}"]
            }
        }
EOF
}

resource "aws_cloudwatch_event_target" "video_conversion_completed" {
  rule = aws_cloudwatch_event_rule.video_conversion_completed.name
  arn  = aws_sqs_queue.video_conversion_job_completed.arn
}

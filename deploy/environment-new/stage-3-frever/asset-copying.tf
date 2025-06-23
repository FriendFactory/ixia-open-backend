resource "aws_sqs_queue" "asset-copying-queue" {
  name                      = "${var.env}-asset-copying"
  max_message_size          = 262144
  receive_wait_time_seconds = 20
}

resource "aws_lambda_function" "asset-copying-lambda" {
  function_name                  = "${var.env}-asset-copying"
  filename                       = "dummy.zip"
  runtime                        = "dotnetcore3.1"
  handler                        = "AssetServer.AssetCopyingLambda::AssetServer.AssetCopyingLambda.Function::Handler"
  role                           = aws_iam_role.asset-copying-role.arn
  reserved_concurrent_executions = 100
  timeout                        = 30
}

resource "aws_lambda_event_source_mapping" "asset-copying-event-mapping" {
  event_source_arn = aws_sqs_queue.asset-copying-queue.arn
  enabled          = true
  function_name    = aws_lambda_function.asset-copying-lambda.arn
  batch_size       = 10
}

resource "aws_iam_role" "asset-copying-role" {
  name = "${var.env}-asset-copying-role"

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
    name = "${var.env}-asset-copying-role-policy"

    policy = jsonencode({
      Version = "2012-10-17"
      Statement = [
        {
          Action   = ["logs:*", "s3:*", "sqs:*"]
          Effect   = "Allow"
          Resource = "*"
        },
      ]
    })
  }
}

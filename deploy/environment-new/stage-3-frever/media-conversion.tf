resource "aws_lambda_function" "media_converter" {
  function_name = "${var.env}-media-converter"

  filename                       = "dummy.zip"
  runtime                        = "dotnetcore3.1"
  handler                        = "ImageConverter::ImageConverter.Function::Handler"
  role                           = aws_iam_role.media_converter_role.arn
  reserved_concurrent_executions = 1
}

resource "aws_iam_role" "media_converter_role" {
  name = "${var.env}-media-converter-role"

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
          Action   = ["logs:*", "mediaconvert:*", "s3:*", "sqs:*"]
          Effect   = "Allow"
          Resource = "*"
        },
      ]
    })
  }
}

data "aws_s3_bucket" "bucket" {
  bucket = var.s3_bucket_name
}

resource "aws_lambda_permission" "allow_bucket" {
  statement_id  = "AllowExecutionFromS3Bucket"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.media_converter.arn
  principal     = "s3.amazonaws.com"
  source_arn    = data.aws_s3_bucket.bucket.arn
}

resource "aws_s3_bucket_notification" "bucket_notification" {
  bucket = data.aws_s3_bucket.bucket.id

  lambda_function {
    lambda_function_arn = aws_lambda_function.media_converter.arn
    events              = ["s3:ObjectCreated:*"]
    filter_prefix       = "Preloaded/"
    filter_suffix       = ".convert"
  }
}

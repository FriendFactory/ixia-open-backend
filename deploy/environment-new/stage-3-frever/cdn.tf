locals {
  domain = "frever-content.com"
}

data "aws_cloudfront_cache_policy" "default" {
  name = "Managed-CachingOptimized"
}

data "aws_s3_bucket" "cdn_source_bucket" {
  bucket = var.s3_bucket_name
}


module "cdn" {
  source                        = "terraform-aws-modules/cloudfront/aws"
  aliases                       = ["${var.env}.${local.domain}"]
  price_class                   = "PriceClass_All"
  retain_on_delete              = false
  wait_for_deployment           = false
  create_origin_access_identity = true

  origin_access_identities = {
    s3_bucket_one = "${var.env}-Cloudfront"
  }

  origin = {
    s3 = {
      domain_name = data.aws_s3_bucket.cdn_source_bucket.bucket_regional_domain_name
      s3_origin_config = {
        origin_access_identity = "s3_bucket_one"
      }
    }
  }

  default_cache_behavior = {
    target_origin_id       = "s3"
    viewer_protocol_policy = "allow-all"

    allowed_methods  = ["GET", "HEAD", "OPTIONS"]
    cached_methods   = ["GET", "HEAD"]
    compress         = true
    query_string     = true
    trusted_signers  = ["self"]
    # cache_policy_id  = data.aws_cloudfront_cache_policy.default.id
  }

  ordered_cache_behavior = [
    {
      path_pattern           = "**/Thumbnail/**"
      target_origin_id       = "s3"
      viewer_protocol_policy = "allow-all"

      allowed_methods = ["GET", "HEAD", "OPTIONS"]
      cached_methods  = ["GET", "HEAD"]
      # cache_policy_id  = data.aws_cloudfront_cache_policy.default.id
    },
    {
      path_pattern           = "**/Thumbnail*.*"
      target_origin_id       = "s3"
      viewer_protocol_policy = "allow-all"

      allowed_methods = ["GET", "HEAD", "OPTIONS"]
      cached_methods  = ["GET", "HEAD"]
      # cache_policy_id  = data.aws_cloudfront_cache_policy.default.id
    }
  ]

  viewer_certificate = {
    # Certificate should be from us-east-1 region for some reasons
    acm_certificate_arn = "arn:aws:acm:us-east-1:722913253728:certificate/d29edfe6-abb6-455b-b9fa-ee5cd6f88428"
    ssl_support_method  = "sni-only"
  }
}


data "aws_iam_policy_document" "s3_policy" {
  statement {
    actions   = ["s3:GetObject"]
    resources = ["${data.aws_s3_bucket.cdn_source_bucket.arn}/*"]

    principals {
      type        = "AWS"
      identifiers = module.cdn.cloudfront_origin_access_identity_iam_arns
    }
  }
}

resource "aws_s3_bucket_policy" "bucket_policy" {
  bucket = data.aws_s3_bucket.cdn_source_bucket.id
  policy = data.aws_iam_policy_document.s3_policy.json
}

data "aws_route53_zone" "frever_content" {
  name = local.domain
}

module "cdn_dns_record" {
  source = "terraform-aws-modules/route53/aws//modules/records"

  zone_id = data.aws_route53_zone.frever_content.zone_id

  records = [
    {
      name = var.env
      type = "A"
      alias = {
        name    = module.cdn.cloudfront_distribution_domain_name
        zone_id = module.cdn.cloudfront_distribution_hosted_zone_id
      }
    },
  ]
}

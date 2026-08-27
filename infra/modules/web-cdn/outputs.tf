output "bucket_name" {
  value       = aws_s3_bucket.web.bucket
  description = "S3 web bucket name."
}

output "bucket_arn" {
  value       = aws_s3_bucket.web.arn
  description = "S3 web bucket ARN."
}

output "cloudfront_distribution_id" {
  value       = aws_cloudfront_distribution.web.id
  description = "CloudFront distribution id."
}

output "cloudfront_distribution_arn" {
  value       = aws_cloudfront_distribution.web.arn
  description = "CloudFront distribution ARN."
}

output "cloudfront_hosted_zone_id" {
  value       = aws_cloudfront_distribution.web.hosted_zone_id
  description = "CloudFront hosted zone id for Route 53 alias records."
}

output "cloudfront_domain_name" {
  value       = aws_cloudfront_distribution.web.domain_name
  description = "CloudFront domain name."
}

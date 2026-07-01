output "bucket_name" {
  value       = aws_s3_bucket.web.bucket
  description = "S3 web bucket name."
}

output "cloudfront_distribution_id" {
  value       = aws_cloudfront_distribution.web.id
  description = "CloudFront distribution id."
}

output "cloudfront_domain_name" {
  value       = aws_cloudfront_distribution.web.domain_name
  description = "CloudFront domain name."
}

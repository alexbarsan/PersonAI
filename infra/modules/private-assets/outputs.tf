output "bucket_name" {
  value       = aws_s3_bucket.assets.bucket
  description = "Private asset bucket name."
}

output "bucket_arn" {
  value       = aws_s3_bucket.assets.arn
  description = "Private asset bucket ARN."
}

output "alerts_topic_arn" {
  value       = aws_sns_topic.alerts.arn
  description = "SNS alerts topic ARN."
}

output "adot_log_group_name" {
  value       = aws_cloudwatch_log_group.adot.name
  description = "ADOT collector log group name."
}

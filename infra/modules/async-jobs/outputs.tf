output "queue_arn" {
  value       = aws_sqs_queue.jobs.arn
  description = "Main asynchronous jobs queue ARN."
}

output "queue_url" {
  value       = aws_sqs_queue.jobs.url
  description = "Main asynchronous jobs queue URL."
}

output "dead_letter_queue_arn" {
  value       = aws_sqs_queue.dead_letter.arn
  description = "Asynchronous jobs dead-letter queue ARN."
}

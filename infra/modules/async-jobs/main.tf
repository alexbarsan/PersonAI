resource "aws_sqs_queue" "dead_letter" {
  name                       = "${var.name_prefix}-jobs-dlq"
  message_retention_seconds  = 1209600
  visibility_timeout_seconds = var.visibility_timeout_seconds
  receive_wait_time_seconds  = 20
  kms_master_key_id          = var.kms_key_arn

  tags = var.tags
}

resource "aws_sqs_queue" "jobs" {
  name                       = "${var.name_prefix}-jobs"
  visibility_timeout_seconds = var.visibility_timeout_seconds
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 20
  kms_master_key_id          = var.kms_key_arn
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dead_letter.arn
    maxReceiveCount     = var.max_receive_count
  })

  tags = var.tags
}

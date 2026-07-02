resource "aws_sns_topic" "alerts" {
  name = "${var.name_prefix}-alerts"

  tags = var.tags
}

resource "aws_sns_topic_subscription" "email" {
  count = var.alert_email == null ? 0 : 1

  topic_arn = aws_sns_topic.alerts.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

resource "aws_cloudwatch_log_group" "adot" {
  name              = "/aws/dreamlens/${var.name_prefix}/adot"
  retention_in_days = 30

  tags = var.tags
}

resource "aws_cloudwatch_dashboard" "operations" {
  dashboard_name = "${var.name_prefix}-operations"

  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "text"
        x      = 0
        y      = 0
        width  = 24
        height = 2
        properties = {
          markdown = "# DreamLens ${var.name_prefix} operations\nAdd API latency, error rate, AI token cost, quota rejection, and database health widgets in S19."
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 2
        width  = 12
        height = 6
        properties = {
          title   = "AI cost"
          region  = data.aws_region.current.name
          metrics = [["DreamLens", "personakit.ai.estimated_cost_usd", { stat = "Sum" }]]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 2
        width  = 12
        height = 6
        properties = {
          title = "Quota and provider guardrails"
          region = data.aws_region.current.name
          metrics = [
            ["DreamLens", "dreamlens.quota.rejections", { stat = "Sum" }],
            ["DreamLens", "dreamlens.rate_limit.rejections", { stat = "Sum" }],
            ["DreamLens", "dreamlens.provider.failures", { stat = "Sum" }]
          ]
        }
      }
    ]
  })
}

resource "aws_cloudwatch_metric_alarm" "ai_cost" {
  alarm_name          = "${var.name_prefix}-ai-cost"
  alarm_description   = "Estimated AI cost exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "personakit.ai.estimated_cost_usd"
  namespace           = "DreamLens"
  period              = 300
  statistic           = "Sum"
  threshold           = var.ai_cost_alarm_threshold_usd
  alarm_actions       = [aws_sns_topic.alerts.arn]
  ok_actions          = [aws_sns_topic.alerts.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "quota_rejections" {
  alarm_name          = "${var.name_prefix}-quota-rejections"
  alarm_description   = "Quota rejections spiked above the expected threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "dreamlens.quota.rejections"
  namespace           = "DreamLens"
  period              = 300
  statistic           = "Sum"
  threshold           = 25
  alarm_actions       = [aws_sns_topic.alerts.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "provider_failures" {
  alarm_name          = "${var.name_prefix}-provider-failures"
  alarm_description   = "AI provider failures exceeded the expected threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "dreamlens.provider.failures"
  namespace           = "DreamLens"
  period              = 300
  statistic           = "Sum"
  threshold           = 5
  alarm_actions       = [aws_sns_topic.alerts.arn]

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "api_error_rate" {
  alarm_name          = "${var.name_prefix}-api-error-rate"
  alarm_description   = "API 5xx count exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "HTTPCode_Target_5XX_Count"
  namespace           = "AWS/ApplicationELB"
  period              = 300
  statistic           = "Sum"
  threshold           = var.error_rate_alarm_threshold
  alarm_actions       = [aws_sns_topic.alerts.arn]

  tags = var.tags
}

data "aws_region" "current" {}

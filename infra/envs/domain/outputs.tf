output "hosted_zone_id" {
  value       = module.domain.hosted_zone_id
  description = "Route 53 hosted zone id."
}

output "hosted_zone_name_servers" {
  value       = module.domain.hosted_zone_name_servers
  description = "Name servers to configure at the domain registrar."
}

output "certificate_arn" {
  value       = module.domain.certificate_arn
  description = "ACM certificate ARN for CloudFront and us-east-1 ALBs."
}

output "certificate_domain_validation_records" {
  value       = module.domain.certificate_domain_validation_records
  description = "DNS records created for ACM certificate validation."
}

output "production_dns_manager_role_arn" {
  value       = try(aws_iam_role.production_dns_manager[0].arn, null)
  description = "Cross-account role that production infrastructure assumes for Dream DNA DNS changes."
}

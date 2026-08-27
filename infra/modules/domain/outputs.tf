output "hosted_zone_id" {
  value       = aws_route53_zone.public.zone_id
  description = "Route 53 hosted zone id."
}

output "hosted_zone_name_servers" {
  value       = aws_route53_zone.public.name_servers
  description = "Name servers to configure at the domain registrar."
}

output "certificate_arn" {
  value       = aws_acm_certificate.public.arn
  description = "ACM certificate ARN."
}

output "certificate_domain_validation_records" {
  value = {
    for name, record in aws_route53_record.certificate_validation : name => {
      name    = record.name
      type    = record.type
      records = record.records
    }
  }
  description = "DNS records created for ACM certificate validation."
}

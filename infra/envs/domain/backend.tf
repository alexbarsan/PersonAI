terraform {
  backend "s3" {
    bucket         = "dreamlens-domain-tfstate-379959319368-us-east-1"
    key            = "dreamlens/domain/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "dreamlens-domain-tflock"
    encrypt        = true
  }
}
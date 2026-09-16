terraform {
  backend "s3" {
    bucket         = "dreamlens-prod-tfstate-097079438907-us-east-1"
    key            = "dreamlens/prod/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "dreamlens-prod-tflock"
    encrypt        = true
  }
}
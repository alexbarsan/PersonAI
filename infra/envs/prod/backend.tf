terraform {
  backend "s3" {
    bucket         = "dreamlens-prod-tfstate-379959319368-us-east-1"
    key            = "dreamlens/prod/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "dreamlens-prod-tflock"
    encrypt        = true
  }
}
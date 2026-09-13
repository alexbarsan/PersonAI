terraform {
  backend "s3" {
    bucket         = "dreamlens-dev-tfstate-379959319368-us-east-1"
    key            = "dreamlens/dev/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "dreamlens-dev-tflock"
    encrypt        = true
  }
}
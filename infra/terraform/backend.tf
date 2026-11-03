terraform {
  required_version = ">= 1.10"

  backend "s3" {
    # Supply bucket and region via -backend-config=marathon.backend.hcl.
    key          = "restaurant/terraform.tfstate"
    use_lockfile = true
    encrypt      = true
  }
}

# Локальна AWS-конфігурація

Оновлено: **3 листопада 2026 року**. Demo вже розгорнуто в **`eu-north-1` (Stockholm)**.

Публічні адреси:

- Frontend: https://dd9nzqrfa7inx.cloudfront.net/
- API: https://kfhwimu435.execute-api.eu-north-1.amazonaws.com/develop

## Параметри окремого середовища

Account ID, назви state/artifacts buckets і локальні профілі належать до конфігурації
конкретного середовища. Не використовуйте значення чужого акаунта.

У `infra/terraform/marathon.auto.tfvars` (gitignored):

```hcl
aws_region = "eu-north-1"
account_id = "<YOUR_AWS_ACCOUNT_ID>"
```

У `infra/terraform/marathon.backend.hcl` (gitignored):

```hcl
bucket = "<YOUR_EXISTING_TERRAFORM_STATE_BUCKET>"
region = "eu-north-1"
```

`backend.tf` задає ключ state, шифрування й lockfile; bucket і регіон передаються
через цей локальний файл. Для наявного deployment використовуйте саме його
state bucket — інший bucket означатиме інший стан Terraform.

## Авторизація інструментів

Використовуйте власний локальний AWS-профіль. Для AWS CLI з browser login:

```powershell
aws login --region eu-north-1 --profile marathon
```

Якщо SDK/Terraform не підтримує `login_session` напряму, сумісний профіль можна
налаштувати через `credential_process`:

```powershell
aws configure set credential_process "aws configure export-credentials --profile marathon --format process" --profile marathon-tools
aws configure set region eu-north-1 --profile marathon-tools
$env:AWS_PROFILE = "marathon-tools"
$env:AWS_REGION = "eu-north-1"
$env:AWS_DEFAULT_REGION = "eu-north-1"
aws sts get-caller-identity
```

Звірте повернутий account із власним середовищем. Credentials зберігаються локально,
не в git. Назви профілів у прикладі довільні.

## AWS Syndicate

Локальний каталог `backend/restaurant-backend-app/.syndicate-config-restaurant-backend-app/`
не публікується. Потрібні `syndicate.yml` і `syndicate_aliases.yml` для вашого середовища:
регіон, account, абсолютний `project_path`, artifacts bucket та aliases ресурсів.
Формат конфігурації описано в [документації Syndicate](https://github.com/epam/aws-syndicate/wiki).
Під час підтримки поточного deployment використовуйте вже налаштовані локальні файли.

Порядок розгортання та перевірки — [DEPLOYMENT_RUNBOOK.md](DEPLOYMENT_RUNBOOK.md).

## Джерела

- [AWS CLI browser sign-in](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sign-in.html)
- [AWS Projects regions](https://docs.aws.amazon.com/accounts/latest/reference/project-regions.html)

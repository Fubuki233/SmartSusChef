# SmartSusChef

## Servers:
```bash
#server only for deployment(no video card, 2GB RAM):
ssh smartsuschef@oversea.zyh111.icu -p 234 

#server for calculation(ML model training, with video card, 64GB RAM):
ssh zyh@oversea.zyh111.icu -p 233

Mysql:
oversea.zyh111.icu:33333
#user: grp4
```

## Weather API:

```bash
https://open-meteo.com/en/docs
```
## Calendar API:

```bash
https://developers.google.cn/workspace/calendar/api/guides/overview
```

## Terraform Deployment:

```bash
cd infrastructure/terraform
terraform init
terraform apply -var-file="environments/uat.tfvars"
```

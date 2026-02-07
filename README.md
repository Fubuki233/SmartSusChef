````markdown
# SmartSusChef

## Quick Start

### Run Dev Environment
Before running the dev environment for the first time, initialize the backend:
```powershell
cd backend
.\setup.ps1
```

Then start all services with:
```powershell
.\dev-start.ps1
```

This will launch three PowerShell windows running:
- **ML Service** on port 8000 (http://localhost:8000)
- **Backend API** on port 5000 (http://localhost:5000)
- **Frontend** on port 5173 (http://localhost:5173)

#### Custom Database Configuration
If you want to use a different database server:
```powershell
.\dev-start.ps1 -DbServer localhost -DbPort 3306 -DbUser root -DbPassword password
```

#### Access Points
- Frontend: http://localhost:5173
- Backend API: http://localhost:5000
- API Documentation: http://localhost:5000/swagger
- ML Service: http://localhost:8000
- ML Documentation: http://localhost:8000/docs

#### Troubleshooting
- If a port is already in use: `netstat -ano | findstr :5173`
- Kill a process: `taskkill /PID <process_id> /F`
- To stop services: simply close the PowerShell windows

---

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

````

#!/bin/bash

# SmartSusChef - Quick Start with Docker
# This script builds and starts all services using Docker Compose

echo "🚀 SmartSusChef - Quick Start"
echo "=============================="
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

# Check if Docker is installed
echo -e "${YELLOW}Checking Docker...${NC}"
if command -v docker &> /dev/null; then
    echo -e "${GREEN}✓ Docker found${NC}"
else
    echo -e "${RED}✗ Docker not found. Please install Docker${NC}"
    exit 1
fi

# Check if Docker Compose is installed
echo -e "${YELLOW}Checking Docker Compose...${NC}"
if command -v docker-compose &> /dev/null; then
    echo -e "${GREEN}✓ Docker Compose found${NC}"
else
    echo -e "${RED}✗ Docker Compose not found. Please install Docker Compose${NC}"
    exit 1
fi

# Stop and remove existing containers
echo -e "\n${YELLOW}Stopping existing containers...${NC}"
docker-compose down

# Build and start all services
echo -e "\n${YELLOW}Building and starting services...${NC}"
docker-compose up -d --build

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Services started successfully${NC}"
else
    echo -e "${RED}✗ Failed to start services${NC}"
    exit 1
fi

# Wait for MySQL to be ready
echo -e "\n${YELLOW}Waiting for MySQL to be ready...${NC}"
sleep 10

# Run database migrations
echo -e "\n${YELLOW}Running database migrations...${NC}"
docker exec smartsuschef_backend dotnet ef database update

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Database migrations completed${NC}"
else
    echo -e "${RED}✗ Database migrations failed${NC}"
fi

# Display service URLs
echo -e "\n${GREEN}=============================="
echo -e "✅ SmartSusChef is running!"
echo -e "==============================${NC}"
echo -e "\n${CYAN}Services:${NC}"
echo -e "  Frontend:    ${NC}http://localhost:3000${NC}"
echo -e "  Backend API: ${NC}http://localhost:5000${NC}"
echo -e "  Swagger UI:  ${NC}http://localhost:5000/swagger${NC}"
echo -e "  MySQL:       ${NC}localhost:3306${NC}"
echo -e "\n${CYAN}Default credentials:${NC}"
echo -e "  Manager:  ${NC}admin / admin123${NC}"
echo -e "  Employee: ${NC}employee / employee123${NC}"
echo -e "\n${CYAN}To view logs:${NC}"
echo -e "  ${NC}docker-compose logs -f${NC}"
echo -e "\n${CYAN}To stop services:${NC}"
echo -e "  ${NC}docker-compose down${NC}"
echo -e "${GREEN}==============================${NC}"

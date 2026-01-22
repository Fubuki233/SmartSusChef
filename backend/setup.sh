#!/bin/bash

# SmartSusChef Backend Setup Script
# Run this script from the backend/SmartSusChef.Api directory

echo "SmartSusChef Backend Setup Script"
echo "==================================="
echo ""

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Check if .NET 8 is installed
echo -e "${YELLOW}Checking .NET SDK...${NC}"
if command -v dotnet &> /dev/null; then
    version=$(dotnet --version)
    echo -e "${GREEN}✓ .NET SDK found: $version${NC}"
else
    echo -e "${RED}✗ .NET SDK not found. Please install .NET 8 SDK${NC}"
    exit 1
fi

# Check if EF Core tools are installed
echo -e "\n${YELLOW}Checking EF Core tools...${NC}"
if dotnet ef --version &> /dev/null; then
    efversion=$(dotnet ef --version)
    echo -e "${GREEN}✓ EF Core tools found: $efversion${NC}"
else
    echo -e "${YELLOW}Installing EF Core tools...${NC}"
    dotnet tool install --global dotnet-ef
fi

# Restore NuGet packages
echo -e "\n${YELLOW}Restoring NuGet packages...${NC}"
if dotnet restore; then
    echo -e "${GREEN}✓ Packages restored${NC}"
else
    echo -e "${RED}✗ Failed to restore packages${NC}"
    exit 1
fi

# Build the project
echo -e "\n${YELLOW}Building project...${NC}"
if dotnet build; then
    echo -e "${GREEN}✓ Build successful${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi

# Create initial migration if it doesn't exist
echo -e "\n${YELLOW}Checking migrations...${NC}"
if [ -d "Migrations" ]; then
    echo -e "${GREEN}✓ Migrations folder exists${NC}"
else
    echo -e "${YELLOW}Creating initial migration...${NC}"
    dotnet ef migrations add InitialCreate
fi

# Prompt for database update
echo -e "\n${YELLOW}Do you want to update the database now? (y/n)${NC}"
read -r updateDb
if [ "$updateDb" = "y" ]; then
    echo -e "\n${YELLOW}Updating database...${NC}"
    if dotnet ef database update; then
        echo -e "${GREEN}✓ Database updated successfully${NC}"
    else
        echo -e "${RED}✗ Database update failed. Please check your connection string in appsettings.json${NC}"
    fi
fi

echo -e "\n${GREEN}===================================${NC}"
echo -e "${GREEN}Setup completed!${NC}"
echo -e "\n${CYAN}To run the application:${NC}"
echo -e "  ${NC}dotnet run${NC}"
echo -e "\n${CYAN}Swagger UI will be available at:${NC}"
echo -e "  ${NC}http://localhost:5000/swagger${NC}"
echo -e "\n${CYAN}Default credentials:${NC}"
echo -e "  ${NC}Manager: admin / admin123${NC}"
echo -e "  ${NC}Employee: employee / employee123${NC}"
echo -e "${GREEN}===================================${NC}"

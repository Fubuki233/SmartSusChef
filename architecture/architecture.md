# Architecture Overview

This is an overview of the architecture of the SmartSus Chef​ system, a Demand Forecasting and Food Prep Recommendation Tool for more sustainable F&B operations​.

## Background
Small and medium F&B operators in Singapore often over‑prepare ingredients to avoid stockouts. ​
However, daily demand can be unpredictable due to weather, special events/holidays, and customer behaviour.​
Many operators lack sophisticated Point-of-Sales (POS) systems, and existing tools are either too complex and/or too costly.​

The gap we’re trying to fill: ​
A simple, flexible demand forecasting tool that works with or without POS systems.​

It works by using data via:​
Manual entry or CSV import​
POS integration (where available)​
Using time-series forecasting​
Incorporating weather and calendar signals​
Offering predictions on future demand to reduce food wastage ​

## 1. Login Page:
Login page:only for authorized staff:
- Staff members can log in using their credentials.
- Successful login redirects to the dashboard.
- Failed login shows an error message.

## 2. Dashboard:

This sector contains two main parts:

### A. Mobile user - Employee
    time range limit: 7 days maximum (today, and last 7 days)
   - sales trend dashboard: combined bar and line chart, displaying the sales trend (sum of all dishes) based on time range (today, last 7 days)
   - input data: allow employee to input only today's sales data and waste data. This can be either by dish or ingredient (weightage).
   - a pie chart to display the breakdown of each dish based on daily sales: after clicking on a single segment in the sale's distribution bar chart, the pie chart will display a breakdown of that day's sales, as per each dish (7 days maximum).
   - a table displaying the sales distribution based on each ingredient: after clicking on a single bar in sales trend chart, the table will display sales distribution of that day (7 days maximum)(format: ingredient unit quantity).
   - calender: display special events/holidays based on the location's public holiday API.
   - weather widget: display current weather based on the location's weather API.
   - prediction detail: display forecasted ingredient quantity (by ingredient: table) for each recipe for next 7 days(editable) based on time series forecasting model(format: ingredient, unit, quantity).
   - wastage trend: will show bar+line chart for wastage trend(by ingredient) based on time range(7 days maximum) and carbon footprint.
   - if user click on bar in wastage chart, a pie chart(by recipe) and a table(by ingredient) will display wastage distribution of that day.

### B. Web user - Manager
    no limit for time range (today, custom range)
   - sales trend dashboard: combined bar and line chart to display sales trend (sum of all dishes) based on time range (today, custom range)
   - input data: allow manager to input any days' sales data / waste data (by dishes) for each recipe. 
   - a pie chart display one day's distribution based on recipe: when click on a single row in sales distribution bar chart, the pie chart will display that daily sales distribution for each dish (recipe).
   - a table display sales distribution based on ingredient: when click on a single bar in sales trend chart, the table will display sales distribution of that day (format: ingredient unit quantity).
   - calender: display special events/holidays based on singapore public holiday API. 
   - weather widget: display current weather based on singapore weather API.
   - prediction summary:  combined bar and line chart, display forecasted total sales quantity for next 7 days (editable) based on time series forecasting model. To also show the trend compared to last week/last holiday)
   - prediction detail: display forecasted ingredient quantity (by ingredient: table) for each recipe for next 7 days (editable) based on time series forecasting model (format: ingredient, unit, quantity).
   - wastage trend: will show chart for wastage trend (by ingredient) based on time range and carbon footprint.
   - button for navigation to Management System.

## 3. Backend Management System:

This system will manage the following functionalities:
A side menu for navigation between different management functions.

1. recipe management:
    this function allows the web user (manager) to manage each dish's recipe:
    ingredients(ingredient, quantity).

2. ingredient management
    this function allows the web user (manager) to manage ingredients:
    add, edit, delete ingredient information.

3. import sales data
    this function allow the web user (manager) to import sales data from local CSV file. in future, we may see if it's possible to connect API to existing POS system.

5. wastage data management
    this function allows web user (manager) to view and edit wastage data. However, a reason needs to be provided when editing historical data, for audit reasons.
    Prototype: to edit on an ingredient basis. By selecting the ingredient, the system will calculate total ingredient wastage. The web user (manager) can edit historical data.
   By MVP: to edit on a dish basis.
   thinking: user cannot edit data that's beyond 7 days (only up to the recent 7 days).

6. sales data management
   this function allows web user (manager) to view and edit sales data. However, a reason needs to be provided when editing historical data, for audit reasons.
   as this is sensitive data, there needs to be additional checks and balances to support the edits.
   optional file upload to be included for audit purposes.
   thinking: user cannot edit data that's beyond 7 days (only up to the recent 7 days).

7. export data
    this function allows web user (manager) to export data:
    export sales data, wastage data, forecast data.

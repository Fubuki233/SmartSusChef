## Remaining Tasks and Reminders for `SmartSusChef` Architecture Alignment

This document outlines the tasks that are still pending to fully align the `SmartSusChef` project with the `_FAST` decoupled ML architecture, following the completion of all Python ML service cleanup and refactoring.

---

### **Phase 1: Integrate C# Backend with ML API**

**Objective:** Enable the C# backend to fetch real ML predictions from the FastAPI `ml-api` service.

1.  **Create `MlForecastService.cs`:**
    *   **Action:** Create a new file `/Users/derek/Documents/GitHub/SmartSusChef/backend/SmartSusChef.Api/Services/MlForecastService.cs`.
    *   **Content:** This service will implement `IForecastService` and make HTTP calls to the `ml-api`.
        *   **Inject `HttpClient`:** The service should inject `HttpClient` and `IConfiguration` (to read `MLService__BaseUrl`).
        *   **Implement `GetForecastAsync` and `GetForecastSummaryAsync`:**
            *   These methods will construct a request object (e.g., `PredictRequestDto`) based on the required forecast parameters (days, location info from `IStoreService`, sales history if needed).
            *   They will then perform an HTTP POST request to the `ml-api` (e.g., `MLService__BaseUrl/predict`).
            *   The response (JSON) from the `ml-api` will be deserialized into C# DTOs (e.g., `ForecastDto`, `ForecastSummaryDto`). You might need to create new DTOs mirroring the `ml-api`'s response structure or adapt existing ones.
        *   **Handle `HttpClient` setup:** Ensure `HttpClient` is properly configured (base address, headers, timeout).

2.  **Update `Program.cs`:**
    *   **Action:** Open `/Users/derek/Documents/GitHub/SmartSusChef/backend/SmartSusChef.Api/Program.cs`.
    *   **Modifications:**
        *   **Register `MlForecastService`:** Change the service registration for `IForecastService` from `MockForecastService` to the new `MlForecastService`.
            ```csharp
            // Old: builder.Services.AddScoped<IForecastService, MockForecastService>();
            builder.Services.AddScoped<IForecastService, MlForecastService>();
            ```
        *   **Configure `HttpClient` for ML Service:** Add an `HttpClient` configuration specifically for `MlForecastService` to read the `MLService__BaseUrl`.
            ```csharp
            builder.Services.AddHttpClient<MlForecastService>(client =>
            {
                var mlServiceBaseUrl = builder.Configuration["MLService:BaseUrl"] ?? throw new InvalidOperationException("MLService:BaseUrl not configured");
                client.BaseAddress = new Uri(mlServiceBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30); // Adjust timeout as needed
            });
            ```
    *   **Reason:** This will switch the C# backend from using mock data to actively requesting predictions from the external FastAPI ML service.

3.  **Review `MockForecastService.cs`:**
    *   **Action:** Delete the file `/Users/derek/Documents/GitHub/SmartSusChef/backend/SmartSusChef.Api/Services/MockForecastService.cs`.
    *   **Reason:** It becomes dead code once `MlForecastService` is implemented and registered.

---

### **Phase 2: Update Architecture Documentation**

**Objective:** Bring the architecture documentation up-to-date with the `_FAST` architecture.

1.  **Overhaul `architecture/smartsuschef_architecture.md`:**
    *   **Action:** Open `/Users/derek/Documents/GitHub/SmartSusChef/architecture/smartsuschef_architecture.md`.
    *   **Modifications:**
        *   **Update Diagram:** Redraw the main architecture diagram to explicitly show:
            *   FastAPI ML Inference Service.
            *   Celery ML Worker Service.
            *   Redis (as broker for Celery).
            *   EFS (for shared ML models).
            *   The C# Backend communicating with the FastAPI ML Inference Service via HTTP.
            *   Clear delineation between the core app and the decoupled ML services.
        *   **Update Rationale:** Explain the benefits of the decoupled architecture (scalability, responsiveness, independent deployment of ML).
        *   **Update Communication Flows:** Revise "Example 2: Manager Views Predictions" to reflect the new flow:
            *   `Web App` calls `Backend - ForecastController`.
            *   `Backend - ForecastController` calls `MlForecastService`.
            *   `MlForecastService` makes HTTP request to `ML Service (FastAPI)`.
            *   `ML Service (FastAPI)` loads model from EFS, runs prediction.
            *   `ML Service (FastAPI)` returns predictions.
            *   `MlForecastService` returns formatted response to `Backend - ForecastController`.
    *   **Reason:** This document is crucial for understanding the system's design. It must accurately reflect the current, more advanced `_FAST` architecture.

2.  **Rename/Recontextualize `architecture/architecture.md`:**
    *   **Action:** Rename `/Users/derek/Documents/GitHub/SmartSusChef/architecture/architecture.md` to something like `architecture/feature_specification.md` or `functional_overview.md`.
    *   **Reason:** Its content is a feature list/functional overview, not a technical architecture document. Renaming it will improve clarity.

---

### **Phase 3: CI/CD Alignment (Conceptual)**

**Objective:** Ensure CI/CD pipelines support the full `_FAST` architecture. (These are next steps/reminders for future implementation.)

1.  **Review `ml-ci-cd.yml` (if it exists) or create one:**
    *   **Action:** Verify/create a CI/CD workflow that:
        *   Triggers on changes in the `ML/` directory.
        *   Builds the `smartsuschef-ml-api` Docker image.
        *   Pushes the image to the ECR repository (`aws_ecr_repository.ml_api`).
        *   Triggers deployment of the `ml-api` and `ml-worker` ECS services.
2.  **Update `web-ci-cd.yml` and `ci-cd.yml` (if they exist) for `backend`:**
    *   **Action:** Ensure that any CI/CD for the `backend` service correctly injects the `MLService:BaseUrl` environment variable pointing to the deployed `ml-api`'s endpoint (e.g., the ALB DNS name with the `/ml` path).
3.  **Ensure Terraform CI/CD Handles New Resources:**
    *   **Action:** Confirm that the Terraform CI/CD pipeline correctly plans and applies changes when the `main.tf` file is updated with the new ElastiCache, EFS, and ML ECS resources.
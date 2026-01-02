# SpaceParts – Fabric Data Platform End-to-End Showcase

## Overview

**SpaceParts** is an end-to-end Microsoft Fabric showcase designed to demonstrate best practices for workspace design, repository structure, and overall data platform architecture.

The solution represents a realistic enterprise scenario with:
- Multiple workspaces
- Multiple data platform layers
- Multiple environments (dev, test, prod)

It includes fully automated CI/CD pipelines that support modern ways of working, validation, and deployment across environments.

## Key Concepts Demonstrated

- Scalable workspace and repository architecture in Microsoft Fabric  
- Environment separation (dev / test / prod)  
- CI/CD pipelines for infrastructure and solution deployment  
- Automated validation and promotion of semantic models  
- Cross-workspace orchestration and reference handling  

## Tooling and Technologies

- **Tabular Editor**
  - Used for validating Best Practice Analyzer (BPA) rules during build validation
  - Used to convert semantic models into Fabric-supported definitions (TMDL)

- **fabric-cicd Python library**
  - Used as the primary build and deployment tool
  - Handles deployment of all Fabric items
  - Manages item references across environments during deployment

## Disclaimer

Use of this code is entirely at your own risk.

- Tabular Editor does not provide official support for this solution
- The solution is intended as inspiration and a boilerplate reference
- No guarantees are provided regarding correctness or future compatibility

## Dataset License

The **SpaceParts dataset** is intended **for learning purposes only**.

- The dataset is the intellectual property of **Tabular Editor ApS**
- It is licensed for **personal, non-commercial use only**
- License terms:  
  https://tabulareditor.com/terms

## Prerequisites

- A Microsoft Fabric capacity (trial or paid SKU)
- Azure DevOps **or** GitHub account with permissions to create:
  - Projects
  - Repositories
- A Service Principal with:
  - Access to Fabric REST APIs  
    https://learn.microsoft.com/en-us/fabric/admin/service-admin-portal-developer
  - Membership in **Project Administrators** (Azure DevOps)

## Initial Solution Setup

1. Create a new Azure DevOps or GitHub project/repository and initialize it with a standard README.
2. Create a new branch, clone the repository locally, and switch to the new branch.
3. Download this project to your local machine.
4. Copy the contents of the `src` folder into the root of your branch folder.
5. Copy `credentials_template.json` to `credentials.json` and adjust the values accordingly.
6. Update the environment recipe files to match your setup.
   - Based on your chosen Git provider, update the `get_settings` block associated with either Azure DevOps or GitHub.
   - Remove the `get_settings` block that is not used from both `feature.json` and `infrastructure.dev.json`.
7. Run the appropriate local setup script (no parameter changes required):
   - **Azure DevOps**:  
     `locale_setup_azuredevops.py`
   - **GitHub**:  
     `locale_setup_github.py`
8. Commit all local changes, create a pull request, and merge into `main`.
9. Run the pipeline/action **Solution IaC – Setup** to provision the Fabric infrastructure.

## Solution Adjustments

After the infrastructure is in place, a few manual steps are required.

### Adjust Orchestration Pipeline

1. Create a new branch named:  
   `feature/orchestrate/adjust_pipeline`

   - The `feature/` prefix triggers creation of feature workspaces
   - The `orchestrate` subfolder limits workspace creation to the orchestration layer

2. Verify that the pipeline **Create feature workspaces** runs and only creates an **Orchestrate** feature workspace.

3. In the newly created workspace (for example:  
   `adjust_pipeline (Orchestrate)`):

   - Open the pipeline **Load Space Parts Demo**
   - Activate all activities
   - Update activity references:
     - **Ingest**
       - Workspace: `SpaceParts - Ingest [dev]`
       - Notebook: `SpaceParts - Ingestion`
     - **Enrich**
       - Workspace: `SpaceParts - Prepare [dev]`
       - Notebook: `SpaceParts - Enrich`
     - **Refresh SpaceParts**
       - Connection: `SpaceParts-SemanticModel`
       - Workspace: `SpaceParts - Model [dev]`
       - Semantic model: `SpaceParts`
   - Save the pipeline
   - Commit changes from the workspace **Source Control** pane

   **Important:**  
   Orchestration pipelines must reference **dev workspaces**, not feature workspaces.  
   This is required for automatic reference replacement during deployment.

### Local Updates

4. Open the solution locally in your preferred IDE (for example VS Code).
5. Switch to the branch `feature/orchestrate/adjust_pipeline`.
6. Run:
   - `locale_update_connections.py`

   This updates:
   - Semantic model definitions
   - Power BI report connections
   - SQL endpoints and model references

7. Verify that multiple files have been updated:
   - `.tmdl`
   - `.json`
   - `.bim`
   - `.pbir`
8. Commit the changes, create a pull request, and merge into `main`.
9. Run:
   - `locale_bind_semantic_model_connection_dev.py`

   This binds the semantic model to the curated lakehouse connection in development and is required for successful test refreshes.

## Running the Solution

You can now run the orchestration pipeline in the **Orchestrate [dev]** workspace.

The pipeline will:
- Extract data into the landing lakehouse
- Transform data into base and curated lakehouses
- Refresh the semantic model used by the SpaceParts demo report

### Workspace Reference Notes

Workspace references in the notebooks **SpaceParts – Ingest** and **SpaceParts – Enrich** are derived automatically.

If you use a different naming convention for feature branches, you must update:

- `DEFAULT_STORAGE_WORKSPACE_PATTERN`

Also verify that all other constants match your environment setup.

## Deployment Options

The solution supports three deployment strategies, available as pipelines (Azure DevOps) or actions (GitHub).

### Solution – Release (Multi-stage)

- Deploys to **test**
- Automatically continues deployment to **production**

### Solution – Release (Single stage)

- Deploys to a single target environment
- Target environment is provided as a runtime parameter

### Solution – Release (Octopus Deploy)

- Creates a temporary branch based on:
  - `main`
  - All feature branches with pending pull requests
- Enables selective promotion to production:
  - All features are always deployed to **test**
  - Only approved and merged features are deployed to **production**

**Azure DevOps specific notes:**

- Update line 97 in `.azure-pipelines/solution_release_octopus.yml` with the pipeline ID of **Solution – Release (Single stage)**
- The pipeline ID can be found in the Azure DevOps URL
- The Build Service must have **Queue build** permissions for the single-stage pipeline
- This is handled automatically by `locale_setup_azuredevops.py`

## Optional: Build Validation and Branch Policies (Azure DevOps)

It is strongly recommended to protect the `main` branch using branch policies.

### Recommended Settings

- Minimum number of required approvers
- Optional requirement for linked work items

### BPA Validation Setup

1. Navigate to **Branches**
2. Select **Branch Policies**
3. Under **Build Validation**:
   - Add pipeline: **PR – BPA Validation**
   - Trigger: Automatic
   - Policy requirement: Required
   - Expiration: 12 hours

If BPA validation fails with severity 3 violations, the pull request will be blocked.

### GitHub Notes

For GitHub, BPA validation is already configured using a `pull_request` trigger targeting `main`.

The configuration is defined in:
- `pr-validation.yaml`



**Note:**  
   On some systems (especially Windows), cloning or working with this repository may fail due to long file paths in the `src` folder.  
   If you encounter path length issues, run the following command before cloning or copying files:

   ```bash
   git config --global core.longpaths true
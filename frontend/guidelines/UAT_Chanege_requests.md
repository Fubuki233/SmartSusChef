# User Acceptance Test (UAT) Log & Change Requests

**Project:** SmartSus Chef  
**Document Type:** UAT Feedback & Test Log  
**Date:** Feb 7, 2026  
**Tester:** Simon (Lei Nuozhen)  

---

## Part 1: Bug Reports (Defects)

### 🔴 Issue ID: BUG-001
**Title:** Numeric Input Display Formatting Issue (Leading Zero Suppression)  
**Module:** Recipe Management -> Edit Recipe -> Recipe Components  
**Severity:** Low (Cosmetic)

* **Description:** When entering a decimal quantity for an ingredient component (e.g., `0.25` kg), the system suppresses the leading zero and displays the value as `.25`. Additionally, users report difficulty typing the digit `0` as the initial character.
* **Steps to Reproduce:**
    1.  Log in as Manager.
    2.  Navigate to **Recipe Management**.
    3.  Click on a recipe to edit (e.g., "Hainan Chicken Rice").
    4.  In the **Recipe Components** section, attempt to input `0.25` in the "QTY" field for an ingredient.
* **Expected Result:** The field should display `0.25` clearly. The user should be able to type `0` easily.
* **Actual Result:** The field displays `.25`. The value is calculated correctly in the backend, but the frontend display is inconsistent with standard formatting.
* **Screenshot:** *(Refer to Image 1 & 2 in original doc)*
* **Recommendation:** Standardize the input mask to allow leading zeros for better readability.

---

### 🔴 Issue ID: BUG-002
**Title:** Input Text Visibility in "Add Component" Search Field  
**Module:** Recipe Management -> Edit Recipe -> Add Component  
**Severity:** Low

* **Description:** When searching for a component (e.g., "Garlic") in the "Add Component" dropdown/text box, the typed text is not visible to the user (white text on white background?), although the system correctly filters/identifies the ingredient in the background.
* **Steps to Reproduce:**
    1.  Open the **Edit Recipe** modal.
    2.  Click **+ Add Component**.
    3.  Click into the "Select Ingredient..." field.
    4.  Type "Garlic".
* **Expected Result:** The user should see the word "Garlic" appearing in the text box as they type.
* **Actual Result:** The text box appears empty (text is invisible), but "Garlic" is selected in the backend logic.
* **Screenshot:** *(Refer to Image 3 in original doc)*
* **Recommendation:** Check the CSS `color` or `z-index` of the input field.

---

## Part 2: Feature Enhancement / Change Request

### 🔵 Issue ID: CR-001 (Change Request)
**Title:** Standardization of Ingredient Creation (Dropdown + "Others" Logic)  
**Module:** Ingredient Management -> Add New Ingredient  
**Priority:** High (Improves Data Quality)

* **Current Behavior:** The user currently types the Ingredient Name manually into a free-text field. This may lead to typos or duplicate entries (e.g., "Egg", "Eggs", "egg").
* **Proposed Requirement:** Refactor the "Add New Ingredient" modal to guide the user:
    1.  **Pre-defined List:** Change the "Ingredient Name" input to a **Searchable Dropdown Menu** (Autocomplete) containing common/standard ingredients.
    2.  **"Others" Option:** Add an option labeled **"Others"** at the bottom of the list.
    3.  **Conditional Logic:**
        * IF the user selects a standard item from the list -> Auto-fill the name.
        * IF the user selects "Others" -> Reveal a new text input field: **"Please specify Ingredient Name"**, allowing manual entry.
* **Business Value:** Prevents data fragmentation and ensures consistent naming for better inventory tracking and predictions.
* **Mockup Reference:** *(Refer to Image 4 in original doc)*
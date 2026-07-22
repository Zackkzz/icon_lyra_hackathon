# Product and build plan

## Product goal

Fridge Meal Planner reduces the planning work that prevents people from cooking
at home. It combines expiry-aware fridge inventory, flexible weekly meal
planning, recipe suggestions, and a generated shopping list.

## Problems and product responses

| User problem | Product response | Current status |
|---|---|---|
| Meal planning is tedious and repetitive | Visual weekly planner with reusable recipes | Partial |
| Shopping requires reconciling many recipe ingredients | Generate a weekly list from planned meals minus fridge stock | Implemented, with same-unit subtraction only |
| Food expires before users decide what to cook | Sort stock by best-before date and recommend matching recipes | Partial |
| Receipt entry is slow | Scan receipts, extract line items, and let the user verify OCR | Planned |
| Healthy, affordable home eating needs too much planning | Preference-aware AI plan with user control and drag/drop editing | Planned |

## Primary users

1. University students with changing class and work schedules.
2. Busy individuals or households trying to reduce food waste.
3. Budget-conscious users who want to cook more often.
4. Health-conscious users who need visible weekly structure without rigid plans.

## Product principles

- The user, not the model, owns the final plan.
- AI output is a proposal until explicitly accepted.
- Expiry priority is visible and explainable.
- Shopping quantities are derived from verified data.
- Manual correction is always available after OCR or AI inference.
- Safety constraints such as allergens are hard rules, not ranking hints.

## MVP scope

### Must have

- Account and household boundary.
- Fridge inventory with quantity, unit, source, and expiry type/date.
- Receipt image upload, OCR extraction, review, and confirmed inventory import.
- Recipe catalogue with normalized ingredients and units.
- Expiry-aware recipe suggestions with missing-ingredient explanations.
- Weekly planner with one authoritative entry per household/date/meal slot.
- Generated shopping list using unit conversions and current stock.
- AI plan proposal, review, accept, edit, and regenerate actions.
- Backend validation for every mutation and AI tool call.

### Should have

- Prepared-meal batches and schedulable portions.
- Dietary preferences, allergens, time, budget, and nutrition targets.
- Pantry/fridge/freezer locations and storage-aware shelf-life estimates.
- Notifications for soon-to-expire food and weekly planning.

### Later

- Barcode lookup, retailer integrations, price comparison, shared live planning,
  nutrition tracking, and personalization from feedback history.

## Delivery plan

### Phase 0 — Stabilize the prototype

- Fix mobile/backend route and request mismatches.
- Repair the current backend compile error in AI recipe suggestions.
- Add validation, error details, and integration tests.
- Add uniqueness and positive-value constraints required by current logic.

**Exit:** the existing fridge, recipe, planner, shopping, conversion, and chat
flows run end-to-end against PostgreSQL.

### Phase 1 — Identity and reliable inventory

- Add authentication, households, membership, and row-level ownership checks.
- Replace aggregate-only fridge rows with inventory lots and immutable events.
- Add explicit expiry type and confidence.
- Add optimistic concurrency or version columns.

**Exit:** multi-user data cannot leak across households and stock changes are
auditable.

### Phase 2 — Receipt OCR

- Upload receipt image to object storage.
- Create asynchronous OCR job and parsed line records.
- Match products/ingredients with confidence scores.
- Require a correction/confirmation screen before inventory writes.

**Exit:** a user can scan, review, correct, and import a receipt without silent
AI writes.

### Phase 3 — AI planning and expiry optimization

- Generate structured proposals constrained by availability, allergies,
  schedule, servings, and expiry urgency.
- Validate proposal IDs, dates, quantities, and authorization on the backend.
- Persist proposal metadata and accept atomically into the meal plan.
- Return reasons, substitutions, and missing items.

**Exit:** a user can generate and safely accept an explainable weekly plan.

### Phase 4 — Shopping and prepared meals

- Convert compatible units before subtracting stock.
- Generate idempotent lists and persist checked state.
- Track cooked batches and portions available for drag/drop scheduling.

**Exit:** the plan, stock, prepared meals, and shopping list remain consistent.

### Phase 5 — Production hardening

- Observability, rate limits, retries, outbox events, backups, and restore test.
- Accessibility, privacy, security, load, and AI evaluation suites.
- Closed beta metrics and iterative ranking improvements.

## MVP success measures

- Median weekly planning time under five minutes.
- At least 70% of generated plans accepted with fewer than five edits.
- Reduced self-reported food disposal after four weeks.
- Shopping lists require fewer than two manual quantity corrections per week.
- No accepted plan violates a confirmed allergen exclusion.


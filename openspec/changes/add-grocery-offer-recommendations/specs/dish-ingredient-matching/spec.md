# Dish ingredient matching

## Purpose

Relate supermarket offers to important ingredients in family dishes while honestly distinguishing saved recipe evidence from confident or uncertain culinary inference.

## ADDED Requirements

### Requirement: Recognise supported ingredient names in both application languages
Explicit ingredient validation SHALL recognise common Danish names and English singular forms for supported canonical ingredients, including oksekød, æg, feta and cherrytomater. Aliases SHALL match whole words, require actual source excerpts for factual claims, and remain subject to family-note exclusions. Vocabulary updates SHALL invalidate previously cached profiles through the profile cache identity.

#### Scenario: Saved Danish recipe contains ordinary ingredients
- **WHEN** a saved recipe explicitly contains Æg, Feta and Cherrytomater
- **THEN** supported factual matches for eggs, cheese and tomatoes are accepted
- **AND** family notes excluding æg still prevent an egg match

### Requirement: Ingredient matches retain confidence and evidence
The system SHALL derive a bounded set of primary, cost-relevant ingredient matches from a dish's current name, family notes, and captured recipe when available. Each match SHALL retain ingredient identity and specificity, ingredient importance, bounded confidence, and whether it is supported by saved evidence or culinary inference. A recognizable dish without a recipe SHALL remain eligible for confident inference; a vague title alone SHALL NOT support invented ingredient matches. Confidence SHALL describe the specific ingredient relationship and SHALL NOT be presented as a measured probability.

#### Scenario: Familiar dish has no recipe
- **WHEN** a dish is named Lasagne and has no contradictory notes or recipe
- **THEN** it can receive a high-confidence inferred minced-meat match
- **AND** the relationship is not presented as a saved recipe fact

#### Scenario: Ingredient alternatives are ambiguous
- **WHEN** a dish is named Curry without further evidence
- **THEN** confidence in a chicken-specific match is lower than for Chicken curry

#### Scenario: Dish lacks culinary identity
- **WHEN** a dish has only an uninformative name and no usable notes or recipe
- **THEN** the system does not invent a primary ingredient to obtain a discount match

### Requirement: Family-specific evidence overrides general assumptions
Explicit family adaptations SHALL take precedence over generic dish-name inference. A captured recipe SHALL take precedence over name-only assumptions unless family notes explicitly adapt it. Contradictory evidence SHALL prevent unsupported matches rather than silently favoring a discount. A general ingredient category SHALL NOT be upgraded to a confirmed specific product, meat species, or preparation. Ingredient inference SHALL NOT overwrite family notes, captured recipes, or confirmed metadata.

#### Scenario: Vegetarian adaptation
- **WHEN** Lasagne has family notes specifying lentils instead of meat
- **THEN** a minced-meat offer does not generate a meat-based boost despite the usual name association
- **AND** source precedence is resolved in the validated ingredient profile before the pure scoring rule consumes it

#### Scenario: General meat category and specific offer
- **WHEN** the dish only supports inferred minced meat and the offer is for minced beef
- **THEN** the match reflects reduced confidence in the specific beef relationship and remains inferred

#### Scenario: Prepared product shares a keyword
- **WHEN** an offer is for prepared meatballs and a dish calls for raw minced beef
- **THEN** the shared meat keyword alone does not establish an equivalent ingredient match

### Requirement: Incidental ingredients do not create recommendation opportunities
Only ingredients with a primary or meaningful cost contribution to the dish SHALL produce an offer boost. Incidental pantry ingredients such as salt SHALL contribute zero even when their presence is certain or their discount percentage is large. Importance SHALL depend on the ingredient's role in the dish rather than merely on keyword presence.

#### Scenario: Salt discount
- **WHEN** salt is discounted by 1 kr and appears in many recipes
- **THEN** those recipes receive no boost from that salt offer

#### Scenario: Significant minced-meat discount
- **WHEN** a dish has a confident primary minced-meat match and a compatible verified 10 percent discount
- **THEN** that offer can generate a positive bounded boost

### Requirement: Derived profiles are reused and invalidated by evidence changes
Ingredient profiles SHALL be reusable across recommendation requests without repeating unchanged inference. Reuse SHALL be family- and dish-scoped and SHALL depend on the current ingredient-relevant evidence and inference version. Changing a title, notes, recipe, or relevant metadata SHALL prevent use of the previous profile for new requests. Changing ratings, history, language, or selected stores alone SHALL NOT require reinferring the dish. Failed or unavailable inference SHALL leave ordinary recommendations usable, with incomplete offer coverage disclosed where relevant.

#### Scenario: Notes change after an earlier inference
- **WHEN** a family updates Lasagne to specify a vegetarian adaptation
- **THEN** new requests cannot reuse its previous meat profile even if that profile is cached

#### Scenario: Locale changes
- **WHEN** the member switches from English to Danish without changing the dish evidence
- **THEN** the profile can be reused while its visible labels use Danish

#### Scenario: Inference is unavailable
- **WHEN** a current profile cannot be obtained within processing limits
- **THEN** that dish remains available for ordinary recommendations without an invented offer match

### Requirement: Profile retention requires no writes on cache hits
Successful profiles SHALL have a fixed 30-day cleanup TTL from publication; reading a cached profile SHALL NOT extend retention or write a last-access timestamp. Failure markers SHALL last 15 minutes. Evidence/version invalidation SHALL remain independent of cleanup retention.

#### Scenario: Frequently reused profile
- **WHEN** an unchanged profile is reused before its fixed retention deadline
- **THEN** it is read without another inference or retention update
- **AND** later requests after expiration may infer and publish a new profile on demand

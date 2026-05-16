---
description: "Use when authoring .frag.xml source files that contain localization entries linked to DescriptionKey properties of game objects. Covers naming conventions and co-location rules for description keys."
applyTo: "**/*.frag.xml"
---

# DescriptionKey Localization Convention

When a game object inside a `<fragment>` declares
`<property name="DescriptionKey" value="someKey"/>`,
the corresponding `<localization key="someKey">` entry must satisfy these rules.

## Naming

- The key for a description localization entry must be derived directly from the
  name of the game object (item, block, entity, etc.) that uses it.
- Required pattern: append the suffix `Desc` to the object name.
  - Item `evSampleItem` → description key `evSampleItemDesc`
  - Item `motorToolPartsA_Alloy` → description key `motorToolPartsA_AlloyDesc`
- Do not use generic, abbreviated, or unrelated key names for description entries.

## Co-location

- Define the `<localization>` entry for a `DescriptionKey` in the **same
  `.frag.xml` file** as the `<fragment>` that contains the object using that key.
- Place the `<localization>` block near the relevant fragment — immediately before
  the fragment declaration is the preferred position.

## Validation Checklist

When reviewing or generating `.frag.xml` content:

1. For every `<property name="DescriptionKey" value="X"/>` inside a `<fragment>`,
   confirm that `<localization key="X">` is defined in the same file.
2. Verify that key `X` follows the `{objectName}Desc` pattern.
3. If the localization entry is absent or the key does not follow the pattern,
   treat it as an error and fix it before finishing.

## Example (correct)

```xml
<!-- Localization entry co-located with the fragment that uses it. -->
<localization key="evSampleItemDesc" file="items" type="Item">
  <english text="A demonstration item."/>
</localization>

<fragment name="localization-sample.items.base" target="items">
  <append xpath="/items">
    <item name="evSampleItem">
      <property name="DescriptionKey" value="evSampleItemDesc"/>
    </item>
  </append>
</fragment>
```

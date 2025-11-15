# Versionize JSON Schema

This directory contains the JSON Schema for versionize configuration files.

## Usage

### Remote Reference

Add the `$schema` property to your `.versionize` file with the GitHub repository URL:

```json
{
  "$schema": "https://raw.githubusercontent.com/versionize/versionize/main/schemas/versionize.schema.json",
  "tagTemplate": "v{version}",
  ...
}
```

## IDE Support

IDEs automatically detect the `$schema` property and provide:
- **IntelliSense**: Auto-completion for properties
- **Validation**: Real-time error checking
- **Hover Documentation**: Descriptions for each property

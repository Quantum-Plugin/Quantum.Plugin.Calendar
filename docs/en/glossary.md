# Glossary

[简体中文](../zh-CN/glossary.md)

<a id="sdk"></a>

## SDK (Software Development Kit)

Published abstractions supplied by Quantum.Plugin.Abstraction.

<a id="rpc"></a>

## RPC (Remote Procedure Call)

Named requests processed through generated clients and servers.

<a id="nof"></a>

## NOF

Framework for generated handlers, dependency injection, mapping and persistence.

<a id="cli"></a>

## CLI (Command Line Interface)

A program operated through commands; Codex is installed and authenticated separately.

<a id="ci"></a>

## CI (Continuous Integration)

Automated build, formatting, test and packaging checks.

<a id="clr"></a>

## CLR (Common Language Runtime)

.NET runtime; assembly and namespace names are compatibility contracts.

<a id="ui"></a>

## UI (User Interface)

Blazor pages, code-behind, styles and localized resources.

<a id="css"></a>

## CSS (Cascading Style Sheets)

Stylesheets, including generated component-scoped bundles.

<a id="migration"></a>

## Migration

An ordered database schema change applied by the host while preserving existing data.

<a id="extension-point"></a>

## Extension point

A host-attributed, versioned channel of optional plugin data. Its owner defines the payload contract; other plugins register, update or withdraw contributions without sharing executable business objects.

<a id="json"></a>

## JSON (JavaScript Object Notation)

A text data format for objects, arrays, strings, numbers, booleans and null. A JSON schema describes the accepted structure; Calendar additionally checks date relationships and domain constraints.

<a id="occurrence"></a>

## Occurrence

One instance of a stored calendar series. Its nominal date comes from the repeat rule; its actual date follows any explicitly selected adjustment. The pair of series ID and nominal date distinguishes instances that move to the same actual date.

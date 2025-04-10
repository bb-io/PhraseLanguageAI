# Blackbird.io Phrase Language AI

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

[Phrase Language AI](https://support.phrase.com/hc/en-us/articles/5709660879516-Phrase-Language-AI-TMS) offers a machine translation (MT) engine capable of providing high-quality translations across multiple languages and ability to understand context.
Phrase Language AI can be used for raw machine translation workflows that require high volumes of translation without any post-editing steps.
The minimum requirement for using Phrase Language AI via API is the Phrase Portal user role.
Phrase Language AI enables file-based translation for the following file types: `.DOCX`, `.TXT`, `.XLSX`, `.CSV`, `.PPTX`, `.HTM/.HTML`, `.XML`, `.JSON`, `.MD`, `.XLF/.XLIFF`, `.SDLXLIFF`.

### MT Autoselect Supported Languages

MT Autoselect supports domain-based recommendation for all documents in these source languages:
- English
- Czech
- German
- Spanish
- French
- Italian
- Japanese
- Korean
- Dutch
- Russian
- Swedish
- Chinese Simplified

All other language combinations get MT Autoselect recommendations but do not take text domain into account.

### Domains 

The type of content translated with machine translation can significantly affect the quality of the output. An engine that might perform well for product descriptions might struggle with a medical report. The content type variable is known as a domain.
Categorizing documents into domains is normally a process that requires human input, creating complications when it comes to scaling translation and allows for basic human error.
Domains were created automatically using an AI-powered analysis of documents. An unsupervised machine learning algorithm recognized 11 distinct types of documents that shared similar sets of keywords:

| Domain                         | Keywords                                                |
|--------------------------------|---------------------------------------------------------|
| Medical                        | study, patients, patient, treatment, dose, mg, clinical |
| Travel and Hospitality         | km, hotel, guests, room, accommodation                  |
| Business and Education         | team, business, work, school, students                  |
| Legal and Finance              | agreement, company, contract, services, financial       |
| Software User Documentation    | click, select, data, text, review, file                 |
| Consumer Electronics           | power, battery, switch, sensor, usb                     |
| Customer Support               | account, email, account, domain, contact                |
| Cloud Services                 | network, server, database, sql, data                    |
| Industrial                     | mm, pressure, valve, machine, oil                       |
| Software Development           | value, class, type, element, string                     |
| Entertainment                  | game, like, get, love, play, go                         |



## Before setting up

Before you can connect you need to make sure that:

- You have a Phrase Language AI [account.](https://phrase.com/)
- You have [organization created](https://support.phrase.com/hc/en-us/articles/6091807042076-Phrase-Platform-Organizations)
- You need to get organizarion ID using one of the ways:
First - you can find in Enabling SSO settings:
![organization ID](Images/README/Option1.png) 

Second - you can find in Profile settings:
![organization ID](Images/README/Option2.png) 

## Connecting

1. Navigate to Apps, and identify the Phrase Language AI app. You can use search to find it.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My Phrase Language AI connection'.
4. Fill in the User name field and Password to your Phrase Language AI account, choose the data center to use and fill the Organization ID.
5. Click _Connect_.

## Actions

### Translation 

- **Translate text** Translates text from source language to target language
- **Translate file** Translates a file
- **Translate file with quality estimation** Gets quality estimation of a translation

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->

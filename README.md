# KnowledgeHubAI — AI Chat with PDF

KnowledgeHubAI is a full-stack AI-powered document assistant that allows users to upload PDF documents, process their content, and ask questions using natural language. The application retrieves relevant document content and generates AI-powered answers with source references to help users understand and explore their documents.

## Features

- **Authentication & Authorization:** User registration, login, email verification, password reset, Google sign-in, and role-based access foundations.
- **PDF Document Management:** Upload, list, search, sort, paginate, delete, and retry document processing.
- **AI-Powered Chat:** Ask questions about uploaded documents and receive answers generated using a Large Language Model (LLM).
- **Source References:** Responses can include relevant document sources, page numbers, and retrieved text.
- **Asynchronous Processing:** RabbitMQ and MassTransit handle background document-processing and email workflows.
- **Vector Search:** Store and retrieve document embeddings using Qdrant.
- **Modern Web Interface:** Angular frontend integrated with the ASP.NET Core API.
- **Session Management:** Secure HttpOnly cookie-based BFF authentication with server-side session storage.

## Architecture Overview

The application uses a layered .NET architecture with a separate Angular frontend and background worker services.

```text
Angular Frontend
       |
       v
ASP.NET Core Web API
       |
       +---- SQL Server
       |
       +---- Cloudflare R2
       |
       +---- RabbitMQ / MassTransit
       |          |
       |          +---- Document Processing Worker
       |          +---- Email Worker
       |
       +---- Qdrant Vector Database
       |
       +---- Ollama / LLM
```

## Technology Stack

### Backend
- C#
- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- MediatR
- FluentValidation
- JWT-based token handling and BFF session authentication

### Frontend
- Angular
- TypeScript
- HTML
- SCSS

### AI & Search
- Large Language Models (LLMs)
- Ollama
- Llama 3.2
- Text embeddings using `intfloat/multilingual-e5-small`
- Qdrant vector database
- PDF text extraction and chunking with PdfPig

### Messaging & Infrastructure
- RabbitMQ
- MassTransit
- Redis-backed BFF sessions
- Cloudflare R2 object storage
- Docker and Rancher Desktop for local container workflows

## How It Works

1. A user signs in and uploads a PDF document.
2. The API stores the document and publishes a processing event.
3. A background worker extracts the PDF text and divides it into smaller chunks.
4. The worker generates embeddings and stores them in Qdrant.
5. The user submits a question through the Angular chat interface.
6. The system retrieves relevant document chunks and uses an LLM to generate an answer with source references.

## Repository Structure

```text
KnowledgeHubAI/
├── KnowledgeHubAI/          # .NET backend solution
│   ├── KnowledgeHubAI.API/
│   ├── KnowledgeHubAI.Application/
│   ├── KnowledgeHubAI.Contracts/
│   ├── KnowledgeHubAI.Domain/
│   ├── KnowledgeHubAI.Infrastructure/
│   ├── KnowledgeHubAI.SharedKernel/
│   ├── KnowledgeHubAI.Workers.DocumentProcessor/
│   ├── KnowledgeHubAI.Workers.Email/
│   └── KnowledgeHubAI.Workers.Notification/
│
├── KnowledgeHubAI-Front/    # Angular frontend
│
└── README.md
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js and npm compatible with the Angular project
- SQL Server
- RabbitMQ
- Qdrant
- Ollama with the required model available
- Access to the configured object-storage and session-storage services

### Backend

1. Clone the repository.
2. Open `KnowledgeHubAI/KnowledgeHubAI.sln` in Visual Studio.
3. Configure the required connection strings and service settings using local configuration or environment variables.
4. Apply the Entity Framework Core migrations to your development database.
5. Start the API and the required background workers.

### Frontend

1. Navigate to the frontend directory:

   ```bash
   cd KnowledgeHubAI-Front
   ```

2. Install dependencies:

   ```bash
   npm install
   ```

3. Start the Angular development server:

   ```bash
   ng serve --ssl
   ```

4. Open the local URL displayed by Angular CLI.

> **Configuration:** Do not commit real passwords, API keys, connection strings, or other secrets. Configure them locally or through environment variables.

## Project Goals

KnowledgeHubAI is a practical full-stack project focused on integrating modern web development with AI-powered document retrieval, background processing, authentication, and vector search.

The project is being developed incrementally, with deployment and further improvements planned as the implementation evolves.

## License

No license has been specified yet. All rights are reserved unless a license is added to this repository.

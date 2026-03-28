# Mud.Feishu Change Log

## [2.0.5] - 2026-03-27

### ✨ Added

- **Spreadsheets**: Added Feishu spreadsheet management module support
  - Worksheet management: create, query, update, delete worksheets
  - Row/Column operations: insert, update, move, delete rows and columns
  - Cell features: merge, find, style settings, data read/write
  - Filter features: filter view creation and management, filter condition settings
  - Data protection: protection range settings and management
  - Data validation: validation rule settings and management
  - Conditional formatting: format rule creation and management
  - Floating images: image insert, query, delete
- **Bitable**: Added Feishu bitable management module support
  - Bitable metadata get and update
- **Authentication**: Added Feishu user authentication module
  - User authentication middleware and context service
  - User authentication test project
- **Demo Project**: Added Feishu task management demo project

### 🔧 Changed

- **Spreadsheet Refactoring**: Refactored Feishu spreadsheet related models and interfaces
  - Refactored spreadsheet related models and interfaces
  - Adjusted spreadsheet interface namespaces and added tenant and user interfaces
  - Refactored cell operation related models and interfaces
  - Renamed cell related classes for better clarity
  - Refactored filter condition related models and interfaces
  - Unified filter view response models and added get interface
  - Refactored spreadsheet data validation interfaces
- **Authentication Refactoring**: Refactored user token manager and dependencies
  - Moved token formatting method to TokenUtils utility class
- **Webhook Refactoring**: Refactored validator base class and utilities
  - Used environment service instead of direct environment variable access
- **Test Optimization**: Used environment service mock instead of environment variable settings
- **Project Optimization**: Removed redundant EnsureUserContext calls and adjusted project references

### 📚 Documentation

- Updated README documentation
- Updated dependency documentation and version notes
- Updated interface documentation comments and added data protection related models
- Improved XML comments

### 📦 Build & Config

- Updated frontend dependency versions
- Updated dependency package versions to 10.0.5 and added authentication tests

## [2.0.4] - 2026-03-19

### ✨ Added

- **Document Management**: Added Feishu document management module support
  - Added Feishu document interfaces and data models
  - Added document block models and interface support
  - Added interfaces and models for getting and batch updating document blocks
  - Added interface for getting paginated list of document block children
  - Added batch delete blocks functionality and related models
  - Added content conversion functionality support
- **Wiki**: Added wiki management functionality
  - Added knowledge space and node management interfaces
  - Added node title update and copy functionality
  - Added node management interfaces and tests
  - Added functionality to move cloud documents to knowledge space
- **Drive Management**: Added Feishu drive management API service support
  - Added drive space folder related interfaces and data models
  - Added drive space file interfaces and metadata models
  - Added file version management functionality
  - Added file upload functionality support
  - Added file import/export task related data models and interfaces
  - Added create file shortcut related models and interfaces
  - Added search cloud documents functionality and related data models
  - Added get cloud document likers list interface
- **Task Module**: Added task and card modules and refactored service registration logic
- **Authentication Enhancement**: Added recoverable flag constructor for FeishuAuthenticationException
- **Enum Configuration**: Extracted enums and configuration classes to independent files

### 🔧 Changed

- **Project Refactoring**: Refactored request and response model file structure
  - Refactored document block model file structure, moved block models to Common directory and deleted original Blocks directory
  - Refactored file operation interface and model naming
  - Refactored code to optimize file upload request models and HTTP client
- **Test Optimization**: Unified data model namespaces in tests and optimized test code
- **Database Optimization**: Added indexes for favorite nodes to improve query performance

### 🐛 Fixed

- **Message Sending**: Fixed issue with request.Content value when sending Multipart/form-data requests
- **Webhook**: Fixed issue where Feishu callback configuration couldn't pass, specified EventType in FeishuEventDecryptor

### 📦 Build & Config

- **Dependency Update**: Updated dependency versions for multiple projects

### 🧪 Tests

- **New Tests**: Added unit tests for Feishu Webhook module

## [2.0.3] - 2026-02-26

### ✨ Added

- **Attendance Management**: Added complete attendance management functionality
  - Leave issuance record related data models and interfaces
  - Clock-in flow record related models and interfaces
  - Attendance archived report related interfaces and data models
  - Correction application related data models and interfaces
  - User face photo upload interface
  - User settings query interface
  - Attendance approval related data models and interfaces
  - Approval status update interface and data models
  - Approval result writing functionality and data models
  - Attendance statistics related data models and interfaces
  - Query statistics data related models and interfaces
  - Query statistics header interface and request models
  - User face recognition information management related interfaces and models
  - Attendance group related interfaces and data models
- **Approval Functionality**: Refactored approval instance preview functionality
- **Card Functionality**: Updated card element request field names
- **Task Functionality**: Updated Feishu task custom field interface return types

### 🔧 Changed

- **Project Structure**: Refactored project structure, adjusted namespaces and file locations
- **Interface Specifications**: Unified interface method naming conventions
- **Attendance Interfaces**: Refactored attendance group interface inheritance structure, renamed attendance user management interface and added download functionality
- **Token Management**: Refactored token management module and updated dependency versions
- **Internationalization**: Refactored internationalization resource models and updated related references
- **Test Structure**: Refactored test file directory structure, moved related test files to corresponding functional module directories
- **Demo Project**: Updated demo project to use package references instead of project references
- **Response Models**: Refactored response models

### 🐛 Fixed

- **User Group Members**: Fixed class name spelling error and updated related references
- **Test Cases**: Fixed test cases and improved test data
- **Activity Subscriptions**: Fixed activity subscription interface return types and improved test cases

### 📚 Documentation

- **Code Comments**: Added XML comments for test classes and cache methods
- **README**: Updated README documentation structure and content

### 📦 Build & Config

- **Dependency Update**: Updated Mud.HttpUtils dependency to version 1.5.3
- **Build Configuration**: Ignored test project build output directories

### 🧪 Tests

- **Improved Test Cases**: Added and improved test cases for multiple modules
  - Chat group and tab tests
  - Feishu task unit tests
  - Feishu user and user group interface tests
  - Feishu position and job level interface tests
  - Employee type related interface tests
  - Feishu department interface tests
  - Task section related tests
  - Task list interface tests
  - Feishu task attachment and comment tests
  - Group announcement related interface tests
  - Card interface tests
  - Feishu approval task related tests
  - Approval query interface tests
  - Approval comment related tests
  - Feishu approval related interface tests
  - Approval message interface tests

## [2.0.2] - 2026-01-30

### ✨ Added

- **Performance Metrics Monitoring**: Added complete performance metrics collection functionality
  - Added MeterExtensions, FeishuMetrics, and FeishuMetricsHelper classes
  - Added metrics recording to key components like TokenManager and HttpClientUtils
- **WebSocket and Webhook Metrics**: Added specialized metrics monitoring for WebSocket and Webhook
  - WebSocket connection count statistics and authentication/event processing metrics
  - Webhook signature verification, event decryption, and processing metrics
  - Event deduplication hit metrics
  - Support for WebSocketConnectionCountProvider to get real-time connection counts
- **Debug Logging Enhancement**: Added response content debug logs and error handling
  - Added debug logging to record raw response content
  - Provided more detailed error messages when JSON deserialization fails
- **Test Controllers**: Added log test controller and network test controller

### 🔧 Changed

- **Dependency Update**: Updated Mud.HttpUtils dependency to version 1.5.2
- **HTTP Client Optimization**:
  - Enhanced exception handling and logging
  - Improved security configuration and connection pool settings
- **Authentication Optimization**: Optimized token acquisition logic and removed redundant metrics recording
  - Changed to record metrics when retrieving token directly from cache
- **Configuration Simplification**: Removed circuit breaker functionality and related code
  - Removed circuit breaker configuration items and documentation
  - Simplified code structure and Webhook service
- **Timestamp Verification**: Optimized timestamp verification logic to prioritize app-specific configuration
- **Demo Project**: Adjusted Demo project configuration and dependencies

### 🐛 Fixed

- **JobLevel Interface**: Changed name parameter of JobLevel interface to nullable type
- **JobFamilies Interface**: Allowed name parameter of GetJobFamilesListAsync to be null

### 📚 Documentation

- Updated README documentation to explain metrics usage
- Added log test related documentation

### 📦 Build & Config

- Updated project version to 2.0.2

## [2.0.0] - 2026-01-28

### 🚨 BREAKING CHANGE

- **Removed FeishuOptions class** - Completely removed old configuration class, all scenarios now use `FeishuAppConfig`
- **Multi-application architecture support** - API signatures and configuration methods changed, migration required
- **Configuration system refactoring** - Retry mechanism and Token management configuration methods changed

### ✨ Added

- **Multi-app support**: Support for configuring and managing multiple Feishu apps
- **App context switching**: New `IFeishuAppContextSwitcher` interface supports runtime app switching
- **Auto-default app inference**: Three auto-inference rules simplify configuration
- **App-level Token cache isolation**: Each app has independent Token cache
- **New configuration parameters**: Added `RetryDelayMs` and `TokenRefreshThreshold` configuration
- **Documentation**: Added configuration migration guide and multi-app configuration documentation

### 🔧 Changed

- **Refactored HttpClient configuration**: Polly retry strategy uses configuration parameters
- **Refactored Token retry logic**: Uses configured `RetryDelayMs` instead of hardcoded values
- **WebSocket retry**: Uses configured `RetryDelayMs` instead of hardcoded values
- **Dependency updates**: Replaced code generator with `Mud.HttpUtils`

### 🐛 Fixed

- **Fixed `RetryCount` configuration inconsistency**: Unified application to HttpClient and TokenManager
- **WebSocket retry hardcoding issue**: Uses configuration parameters instead of hardcoded values

---

## 1.2.2 (2026-01-19)

### 🚀 Feature Enhancements

#### Attendance Management API

- **New Attendance Management API Service Support**
  - Files: Attendance-related modules
  - Impact: Provides complete attendance shift management capabilities

- **Shift Management Interfaces**
  - Added: Shift query by name interface
  - Added: Shift details interface and related data models
  - Added: Shift deletion interface and corrected shift creation interface path
  - Added: Attendance shift related data models and interfaces
  - Impact: Supports complete shift operations including creation, query, deletion, etc.

#### Approval Function Extension

- **Approval Message API**
  - Added: Approval Bot message update interface and request models
  - Added: Approval Bot message related data models and interfaces
  - Impact: Supports real-time update and management of approval messages

- **Approval Task Query**
  - Added: Approval task query interface and related data models
  - Impact: Provides more comprehensive approval task management capabilities

#### Demo Project Enhancement

- **Feishu OAuth Login Demo**
  - Added: Feishu OAuth login demo project
  - Impact: Provides complete OAuth login integration example

### 🐛 Bug Fixes

- **Decryption Failure Handling**
  - Fixed: Null reference issue when handling verification requests during decryption failure
  - Impact: Improved stability of decryption exception handling

- **Token Management Fix**
  - Fixed: User token management and status cleanup issues
  - Impact: Ensures proper token management and release

- **Project File Fix**
  - Fixed: Duplicate closing issue with PackageTags tag in project files
  - Impact: Resolves project file format issues

- **Webhook Middleware Fix**
  - Fixed: Removed verification request attributes and fixed middleware indentation
  - Impact: Optimizes Webhook middleware code structure

### 🔧 Code Refactoring

#### Model and Interface Refactoring

- **Shift Model Refactoring**
  - Refactored: Shift-related models, extracted common base class
  - Impact: Improves code reusability and maintainability

- **Authentication Service Refactoring**
  - Refactored: Authentication service and approval query interfaces
  - Impact: Optimizes service architecture

- **Namespace and Interface Renaming**
  - Refactored: Renamed IFeishuV3AuthenticationApi to IFeishuV3Authentication
  - Impact: Unifies interface naming conventions

#### Project Structure Optimization

- **File Organization Improvement**
  - Refactored: Moved project files to Sources folder to improve organization structure
  - Impact: Optimizes project directory structure

- **Tool Class Refactoring**
  - Refactored: Moved ExceptionUtils and HttpClientUtils to Utilities namespace
  - Refactored: Restructured log desensitization functionality and centralized into common utility class
  - Impact: Unifies tool class management

#### Performance and Security Optimization

- **Redis Command Optimization**
  - Refactored: Uses SCAN instead of KEYS command to avoid blocking Redis service
  - Impact: Improves performance and security of Redis operations

- **Cache Management Optimization**
  - Refactored: Merged methods for generating cache keys and added support for user ID parameters
  - Refactored: Restructured token manager's caching and formatting logic
  - Refactored: Restructured token management module, introduced cache abstraction layer
  - Impact: Optimizes cache management logic

- **Code Cleanup**
  - Refactored: Removed unused variables and redundant using declarations
  - Refactored: Removed standalone health check extension class and simplified registration logic
  - Refactored: Renamed health check namespace and updated references
  - Impact: Improves code quality and maintainability

### 📝 Documentation Improvements

- **API Documentation Updates**
  - Improved: Comments for approval message data models
  - Impact: Enhances API documentation completeness

- **Project Documentation Updates**
  - Updated: Project clone links in README
  - Updated: Repository links and version numbers in documentation
  - Added: README documentation for Mud.Feishu demo project collection
  - Impact: Provides more accurate project information

- **Demo Documentation Optimization**
  - Removed: WebSocket interceptor documentation and updated Webhook documentation
  - Updated: README documentation for Feishu WebSocket demo project
  - Moved: Feishu OAuth demo README file location
  - Impact: Optimizes demo documentation structure

### 📦 Build and Configuration

- **Version Update**
  - Updated: Project version to 1.2.2 and synchronized dependencies
  - Impact: Releases new version

- **Dependency Management Optimization**
  - Updated: Moved HTTP-related dependencies from global to specific projects
  - Impact: Optimizes dependency management

- **Git Configuration Update**
  - Updated: .gitignore file to exclude sensitive configurations and publish directories
  - Impact: Improves version control security

## 1.2.1 (2026-01-16)

**Type**: Configuration enhancement, security hardening, code quality improvement

### 🔒 Security Enhancements

#### Required Field Validation Strengthening

- **FeishuAppConfig Configuration Validation**
  - File: `Mud.Feishu.Abstractions/Configuration/FeishuAppConfig.cs`
  - Added: AppId format validation (must start with `cli_` or `app_`)
  - Added: AppId length validation (minimum 20 characters)
  - Added: AppSecret length validation (minimum 16 characters)
  - Added: Data Annotations attributes (`[Required]`, `[RegularExpression]`, `[MinLength]`)
  - Impact: Configuration errors can be detected at startup, avoiding runtime exceptions

- **FeishuWebhookOptions Configuration Validation**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Added: EncryptKey length validation (must be 32 characters)
  - Added: Data Annotations attributes to required fields
  - Impact: Ensures correct Feishu event encryption key configuration

#### Sensitive Information Protection

- **Configuration Class Sensitive Information Masking**
  - Files:
    - `Mud.Feishu.Abstractions/Configuration/FeishuAppConfig.cs`
    - `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
    - `Mud.Feishu.Redis/Configuration/RedisOptions.cs`
  - Added: `ToString()` method override, automatically masks sensitive information
  - Implementation: Shows first and last 2 characters, replaces middle with `****`
  - Impact: Prevents accidental leakage of sensitive information in logs

- **Redis Configuration Validation**
  - File: `Mud.Feishu.Redis/Configuration/RedisOptions.cs`
  - Added: ServerAddress format validation
  - Added: Connection timeout parameter range validation
  - Added: Data Annotations attributes
  - Impact: Ensures correct Redis connection configuration

### 🔧 Configuration Optimization

#### Configuration Example File

- **appsettings.example.json**
  - File: `appsettings.example.json` (new)
  - Added: Complete configuration example file
  - Contains: Feishu, FeishuWebhook, FeishuWebSocket, Redis configurations
  - Impact: New users can quickly understand configuration structure

### 🧪 Test Coverage

#### Configuration Unit Tests

- **FeishuAppConfig Tests**
  - File: `Tests/Mud.Feishu.Abstractions.Tests/Configuration/FeishuAppConfigTests.cs`
  - Coverage: AppId/AppSecret validation, range restrictions, sensitive information masking, auto-inference logic
  - Test cases:
    - Valid configuration validation
    - AppId format validation (cli*/app* prefix)
    - AppId/AppSecret null value validation
    - AppId/AppSecret length validation
    - TimeOut/RetryCount range restrictions
    - BaseUrl format validation
    - ToString sensitive information masking
  - Impact: Core configuration logic test coverage

### 📝 Documentation Improvements

#### XML Documentation Comment Improvements

- **Configuration Class Documentation Updates**
  - Files: All Options configuration classes
  - Updated: Added complete parameter descriptions and example values
  - Added: Data Annotations error message descriptions
  - Impact: Improves code readability and IDE intellisense

### 🔨 Breaking Changes

- Modified `FeishuAppConfig.Validate()` method, now validates AppId/AppSecret format
- Added `FeishuWebhookOptions.EncryptKey` length validation (32 characters)
- Added `RedisOptions.Validate()` method, validates connection parameters

### 📦 Dependency Updates

- New dependencies:
  - `System.ComponentModel.DataAnnotations` (for Data Annotations)

## 1.1.2 (2025-11-17)

**Type**: Feature enhancement, code refactoring, bug fixes, documentation improvement

### 🚀 Feature Enhancements

- **API Service Support**
  - Added: Attendance management API service support
  - Added: Shift management related interfaces
  - Added: Approval message API and approval task query interfaces
  - Impact: Provides more comprehensive Feishu functionality integration

- **Project Structure Optimization**
  - Added: Moved project files to Sources folder
  - Impact: Improves project organization structure

### 🐛 Bug Fixes

- **Decryption Failure Handling**
  - Fixed: Null reference issue when handling verification requests during decryption failure
  - Impact: Improves exception handling stability

- **User Token Management**
  - Fixed: User token management and status cleanup issues
  - Impact: Ensures proper token management and release

### 🔧 Code Refactoring

- **Authentication Service Refactoring**
  - Refactored: Authentication service and approval query interfaces
  - Refactored: Renamed IFeishuV3AuthenticationApi to IFeishuV3Authentication
  - Impact: Unifies interface naming conventions

- **Redis Command Optimization**
  - Refactored: Uses SCAN instead of KEYS command to avoid blocking Redis service
  - Impact: Improves performance and security of Redis operations

- **Cache Management Optimization**
  - Refactored: Merged methods for generating cache keys and added support for user ID parameters
  - Refactored: Restructured token manager's caching and formatting logic
  - Impact: Optimizes cache management logic

### 📝 Documentation Improvements

- **API Documentation Updates**
  - Improved: Comments for approval message data models
  - Impact: Enhances API documentation completeness

- **Project Documentation Updates**
  - Updated: Project clone links in README
  - Updated: Repository links and version numbers in documentation
  - Impact: Provides more accurate project information

## 1.1.1 (2026-01-06)

**Type**: Feature enhancement, code refactoring, documentation improvement

### 🚀 Feature Enhancements

- **Approval Function Extension**
  - Added: Approval event type constants (WorkApproval and WorkApprovalRevert)
  - Added: Interface to get detailed data by approval definition Code
  - Added: Third-party approval definition creation functionality
  - Added: Approval instance comment pagination query interface
  - Added: RemoveCommentsAsync interface to clear all comments and approval replies under approval instance
  - Added: Approval comment deletion functionality and optimized comment creation parameters
  - Added: Approval task signature functionality
  - Added: Interface to get detailed data by approval definition Code for approval tasks
  - Added: Approval instance status pagination query interface
  - Impact: Provides more complete approval process management capabilities

- **Task Management Functionality**
  - Added: Custom field management interface
  - Added: Custom field option management
  - Added: Custom field resource binding
  - Added: Custom field list pagination query
  - Impact: Supports custom field management for tasks

### 🔧 Code Refactoring

- **API Response Type Unification**
  - Refactored: Updated all API response types to FeishuApiResult series
  - Impact: Unifies API response format

- **Message Sending Interface Refactoring**
  - Refactored: Unified message sending interface design
  - Impact: Improves consistency of message sending functionality

- **Service Registration API Unification**
  - Refactored: Unified service registration API and removed redundant UseMultiHandler method
  - Impact: Simplifies service registration process

### 📝 Documentation Improvements

- **README Documentation Optimization**
  - Updated: Removed architecture design and performance characteristics documentation
  - Updated: Added department event handler documentation and usage examples
  - Updated: Optimized project description and feature explanations
  - Impact: Improves documentation practicality and readability

## 1.1.2 (2026-01-10)

**Type**: Feature enhancement, code refactoring, bug fixes, documentation improvement

### 🚀 Feature Enhancements

- **Webhook and WebSocket Functionality**
  - Added: Webhook verification method changed to async implementation and refactored retry service
  - Added: WebSocket event retry functionality, supporting both distributed and in-memory modes
  - Added: Event processing state synchronization and reconnection mechanism
  - Added: Request body signature verification option
  - Impact: Improves reliability and stability of event processing

- **Approval Function Extension**
  - Added: Third-party approval instance verification functionality
  - Added: Third-party approval instance synchronization functionality
  - Added: Approval instance status pagination query interface
  - Impact: Enhances completeness of approval functionality

- **Configuration Verification Functionality**
  - Added: WebSocket and Feishu configuration options
  - Added: Webhook configuration verification functionality and optimized option documentation
  - Impact: Improves configuration correctness and security

### 🐛 Bug Fixes

- **Project Configuration Fix**
  - Fixed: WebSocket configuration-related issues
  - Impact: Ensures WebSocket functionality operates normally

### 🔧 Code Refactoring

- **Event Handler Improvement**
  - Refactored: Adjusted event handler base class design to support async processing
  - Refactored: Updated event handler base class to async handler
  - Impact: Improves efficiency of event processing

- **Redis Service Registration**
  - Refactored: Simplified Redis service registration method and updated documentation
  - Impact: Simplifies usage of Redis service

- **Service Registration Refactoring**
  - Refactored: Renamed FeishuWebhook service registration method name
  - Refactored: Refactored Feishu service registration method naming
  - Impact: Unifies service registration method naming conventions

### 📝 Documentation Improvements

- **English Documentation Restructuring**
  - Updated: Reorganized English documentation structure and restored feature characteristic explanations
  - Impact: Improves readability of English documentation

### 📦 Dependency Updates

- Updated: Project dependency package versions to 1.1.2
- Updated: Mud.ServiceCodeGenerator version
- Impact: Ensures compatibility and stability of dependency packages

## 1.1.0 (2025-11-12)

**Type**: Feature enhancement, code refactoring, documentation improvement

### 🚀 Feature Enhancements

- **User Management API**
  - Added: Complete user management interfaces including create, update, delete users
  - Added: Batch query user information interfaces
  - Added: Restore deleted user interfaces
  - Impact: Provides comprehensive user management capabilities

- **User Group Management API**
  - Added: User group creation, update, deletion interfaces
  - Added: Query user's group membership and user group details interfaces
  - Impact: Supports complete user group management

- **Department Management API**
  - Added: Department creation, update, deletion interfaces
  - Added: Query departments by parent ID, query parent departments by department ID interfaces
  - Impact: Provides complete department management functionality

- **Employee Type Management API**
  - Added: Employee type related interfaces
  - Impact: Supports employee type management

### 🔧 Code Refactoring

- **API Result Model Refactoring**
  - Refactored: ApiListResult list data result collection
  - Refactored: Renamed ApiResult to FeishuApiResult
  - Impact: Unifies API result model naming

- **Service Registration Optimization**
  - Refactored: Service registration code structure
  - Impact: Improves service registration efficiency

### 📝 Documentation Improvements

- **README Documentation Optimization**
  - Updated: README file content and structure
  - Added: Project feature descriptions and usage examples
  - Impact: Improves documentation readability

## 1.0.9 (2025-11-14)

**Type**: Feature enhancement, code refactoring, bug fixes

### 🚀 Feature Enhancements

- **Cross-platform Support**
  - Added: Support for .NET Standard 2.0
  - Impact: Improves framework compatibility

- **HTTP Client Enhancement**
  - Added: Feishu HTTP client extension methods
  - Added: Enhanced HTTP client configuration
  - Impact: Provides more flexible HTTP request configuration

### 🐛 Bug Fixes

- **HttpClient Configuration Fix**
  - Fixed: HttpClient configuration and API endpoint URL format issues
  - Impact: Ensures API requests are sent correctly

### 🔧 Code Refactoring

- **Message and Event API Refactoring**
  - Refactored: Message and event API implementation
  - Impact: Improves code maintainability

- **File Download Function Refactoring**
  - Refactored: File download functionality and optimized HTTP request methods
  - Impact: Improves file download performance

## 1.0.7 (2025-11-12)

**Type**: Feature enhancement, code refactoring, documentation improvement

### 🚀 Feature Enhancements

- **Task Management API**
  - Added: Task comment related interfaces
  - Added: Task attachment management interfaces
  - Added: Task activity subscription interfaces
  - Added: Task list member management interfaces
  - Impact: Provides complete task management capabilities

- **JsTicket API**
  - Added: JsTicket API interfaces
  - Impact: Supports front-end JavaScript development requirements

### 🔧 Code Refactoring

- **Code Generator Upgrade**
  - Upgraded: Mud.ServiceCodeGenerator version
  - Impact: Improves code generation efficiency and quality

- **Project Dependency Updates**
  - Updated: Project dependency configuration and removed test tools
  - Impact: Optimizes project dependency management

### 📝 Documentation Improvements

- **Task Related Documentation Updates**
  - Updated: Task member information comments
  - Impact: Improves API documentation accuracy

## 1.0.3-dev (2025-11-12)

**Type**: Initial version, feature enhancement

### 🚀 Feature Enhancements

- **Core Function Implementation**
  - Added: Feishu API basic framework setup
  - Added: Authentication service and token management
  - Added: Enterprise address book related interfaces
  - Added: Message sending and receiving functionality
  - Added: Webhook and WebSocket support
  - Impact: Establishes project basic architecture

- **Demo Projects**
  - Added: Webhook demo service
  - Added: WebSocket demo project
  - Impact: Provides usage examples

### 📝 Documentation Improvements

- **Project Documentation Initialization**
  - Added: README.md file
  - Added: Project structure description
  - Impact: Provides project basic documentation

## 1.2.0 (2026-02-15)

**Type**: Feature enhancement, security hardening, performance optimization

### 🚀 New Features

#### Security Enhancements

- **Content Type Validation**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Added: Request Content-Type validation, only accepts `application/json`
  - Prevention: Maliciously constructed non-JSON requests
  - Impact: Improved request security

- **JSON Depth Limitation**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - Added: `MaxDepth = 64` limit to prevent deeply nested JSON
  - Prevention: DoS attacks and stack overflow risks
  - Impact: Deserialization security

- **Event Processing Interceptor**
  - File: `Mud.Feishu.Abstractions/IFeishuEventInterceptor.cs` (new)
  - Added: Pre/post event processing interceptor mechanism
  - Support: Logging, performance monitoring, custom validation
  - Impact: Enhanced extensibility

- **Failed Event Retry Service**
  - File: `Mud.Feishu.Webhook/Services/FailedEventRetryService.cs` (new)
  - Added: Background automatic retry of failed events
  - Strategy: Exponential backoff (2^retryCount minutes, max 60 minutes)
  - Impact: Improved event processing reliability

- **Circuit Breaker Pattern**
  - File: `Mud.Feishu.Webhook/Services/FeishuWebhookService.cs`
  - Added: Circuit breaker pattern implemented with Polly
  - Configuration: Break after 5 consecutive failures, retry after 30 seconds
  - Impact: Improved system stability

#### Performance Optimizations

- **Streaming Request Body Reading**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Optimization: Stream reading of request body, real-time size validation
  - Prevention: DoS attacks with forged Content-Length
  - Impact: Memory usage optimization

- **Full Application of Source Generator Serialization**
  - File: All serialization/deserialization code
  - Optimization: Full use of `JsonSerializerContext`
  - Performance: ~20-30% improvement
  - Impact: All JSON operations

#### Observability Enhancement

- **Extended Metrics Collection**
  - File: `Mud.Feishu.Webhook/Models/MetricsCollector.cs`
  - Added:
    - `feishu_webhook_events_received_total`
    - `feishu_webhook_events_failed_total`
    - `feishu_webhook_event_processing_duration_seconds`
    - `feishu_webhook_active_requests`
    - `feishu_webhook_circuit_breaker_state`
  - Impact: Better monitoring capabilities

- **Log Sanitization Middleware**
  - File: `Mud.Feishu.Webhook/Utils/LogSanitizer.cs` (new)
  - Added: Automatic sanitization of sensitive fields (encrypt, signature, token, etc.)
  - Prevention: Sensitive information leakage to logs
  - Impact: Production log security

### 🔒 Security Fixes

#### High-Risk Issues Fixed

- **[CVE-2026-XXXX] Nonce Expiration Cleanup Mechanism**
  - File: `Mud.Feishu.Abstractions/IFeishuNonceDistributedDeduplicator.cs`
  - Issue: Nonce storage lacks automatic expiration cleanup, may cause memory leaks
  - Fix: Added timestamp-based TTL cleanup mechanism
  - Implementation: Clean expired Nonces during each query
  - Impact: All scenarios using Nonce deduplication

- **[CVE-2026-XXXX] Request Size Validation Bypass**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: Only checked Content-Length header, bypassable by forgery
  - Fix: Stream reading of request body, real-time size validation
  - Impact: Prevention of DoS attacks

- **[CVE-2026-XXXX] Production Environment Configuration Bypass**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Issue: Incomplete environment variable detection, may bypass security checks
  - Fix: Enhanced environment detection, supports multiple environment identifiers
  - Impact: Production environment security configuration

#### Medium-Risk Issues Fixed

- **JSON Deserialization Depth Limitation**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - Issue: Missing MaxDepth limit, may cause performance issues
  - Fix: Added MaxDepth = 64 limit
  - Impact: Deserialization security

- **Rate Limiter Memory Management Optimization**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuRateLimitMiddleware.cs`
  - Issue: Dictionary may grow infinitely, lacks capacity limit
  - Fix: Added maximum entry limit (100k) and LRU eviction
  - Impact: Rate limiter stability

- **Concurrency Control Resource Management**
  - File: `Mud.Feishu.Webhook/Services/FeishuWebhookConcurrencyService.cs`
  - Issue: Task.Run didn't pass cancellation token, may not release correctly
  - Fix: Implemented IHostedService, added application shutdown hooks
  - Impact: Resource release during service shutdown

### 🐛 Bug Fixes

#### Resource Management Fixes

- **AES/SHA256 Resource Leak**
  - Files:
    - `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
    - `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Fix: Use `using` statements to ensure resource release
  - Impact: Encryption/decryption operations

- **Concurrency Control Resource Leak**
  - File: `Mud.Feishu.Webhook/Services/FeishuWebhookConcurrencyService.cs`
  - Fix: Implemented IHostedService lifecycle management
  - Impact: Resource release during service shutdown

#### Logic Fixes

- **Configuration Change Event Notification**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Fix: Added OptionsChanged event
  - Impact: Real-time notification of configuration hot updates

### 📝 Documentation Improvements

#### New Documentation

- **docs/troubleshooting.md**
  - Troubleshooting guide
  - FAQ (Frequently Asked Questions)
  - Diagnostic flowchart

- **docs/performance-tuning.md**
  - Performance tuning guide
  - Concurrency configuration suggestions
  - Monitoring metric explanations

- **docs/security-best-practices.md**
  - Security best practices
  - Production environment configuration checklist
  - Risk assessment recommendations

#### Improved Documentation

- **README.md**
  - Updated configuration instructions
  - Added troubleshooting links
  - Supplemented performance optimization suggestions

- **SECURITY.md**
  - Updated security configuration requirements
  - Added CVE disclosure process
  - Supplemented security audit reports

### 🔄 Breaking Changes

#### API Changes

- **IFeishuEventHandlerFactory Extension**
  - New methods: `GetHandlerInfo()`, `ClearHandlers()`
  - Impact: Classes implementing the interface

- **FeishuWebhookOptions Extension**
  - New properties: `MaxRetryCount`, `CircuitBreakerEnabled`
  - Impact: Configuration files need updates

#### Configuration Changes

- **New Required Configuration**
  - `MaxRetryCount`: Default value 3
  - `CircuitBreakerEnabled`: Default value true
  - `MaxRequestBodySize`: Default value 10MB

### ⚠️ Migration Guide

If you are upgrading to v1.2.0, please update your configuration files:

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true,
    "TimestampToleranceSeconds": 60,
    "MaxConcurrentEvents": 10,
    "EventHandlingTimeoutMs": 30000,
    "EnableBackgroundProcessing": false,
    "MaxRetryCount": 3,
    "CircuitBreakerEnabled": true,
    "RateLimit": {
      "EnableRateLimit": true,
      "MaxRequestsPerWindow": 100,
      "WindowSizeSeconds": 60,
      "MaxIpEntries": 100000
    }
  }
}
```

If using interceptors, register them in `Program.cs`:

```csharp
builder.Services.AddFeishuWebhook(options =>
{
    // ... existing configuration ...
})
.AddEventInterceptor<LoggingEventInterceptor>()
.AddEventInterceptor<MetricEventInterceptor>();
```

#### Dependency Updates

New dependencies:

- `Polly`: for circuit breaker pattern

```bash
dotnet add package Polly
```

### 🧪 Testing

#### New Unit Tests

- Nonce deduplication cleanup tests
- Content-Type validation tests
- JSON depth limitation tests
- Streaming request body reading tests
- Interceptor execution order tests
- Circuit breaker state transition tests
- Exponential backoff retry tests
- Log sanitization tests
- Rate limiter LRU eviction tests
- Concurrency control resource release tests

#### New Integration Tests

- End-to-end Webhook request tests
- Failed event retry flow tests
- Circuit breaker fault recovery tests
- Distributed scenario tests (multi-instance)

#### Performance Tests

- 100k concurrent request tests
- 1MB request body processing tests
- Deeply nested JSON processing tests
- Memory leak stress tests

### 📦 Dependency Updates

#### New Dependencies

- `Polly`: 8.4.0
  - Purpose: Circuit breaker pattern implementation
  - License: BSD-3-Clause

#### Updated Dependencies

- `Microsoft.Extensions.Diagnostics.HealthChecks`: 8.0.x
- `Microsoft.Extensions.Logging.Abstractions`: 8.0.x
- `System.Text.Json`: 8.0.x

---

## 1.1.3 (2026-01-15)

**Type**: Bug fix and security enhancement

### 🔒 Security Fixes

#### High-Risk Issues Fixed

- **[CVE-2026-XXXX] Production Environment Signature Validation Check Restoration**
  - File: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - Issue: Production environment forced signature validation logic was commented out, posing serious security risk
  - Fix: Uncommented lines 197-202 to ensure production environment signature validation is enabled
  - Impact: All Webhook users

- **[CVE-2026-XXXX] Background Processing Failed Event Persistence**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: During background processing failure, condition judgment error caused failed events to not be persisted
  - Fix: Corrected line 583 condition judgment, implemented TODO comment (lines 601-602)
  - Impact: All users of background processing mode

- **[CVE-2026-XXXX] Demo Project Hardcoded Sensitive Keys**
  - File: `Mud.Feishu.Webhook.Demo/appsettings.json`
  - Issue: VerificationToken and EncryptKey hardcoded in configuration file
  - Fix: Removed hardcoded values, added SECURITY-WARNING.md documentation
  - Impact: Demo project users

#### Medium-Risk Issues Fixed

- **WebSocket Error Handling Enhancement**
  - File: `Mud.Feishu.WebSocket/FeishuWebSocketClient.cs`
  - Issue: Exception handling not detailed enough, lacking classification and recovery judgment
  - Fix: Added detailed exception classification handling, distinguishing recoverable and non-recoverable errors
  - Impact: WebSocket connection stability

- **Redis Connection Failure Fallback Handling**
  - File: `Mud.Feishu.Redis/Services/RedisFeishuEventDistributedDeduplicatorWithFallback.cs` (new)
  - Issue: No fallback strategy when Redis connection fails
  - Fix: Implemented automatic fallback to memory deduplication, supporting exponential backoff retry
  - Impact: Users of Redis distributed deduplication

- **Authentication Failure Handling Enhancement**
  - File: `Mud.Feishu.WebSocket/Core/AuthenticationManager.cs`
  - Issue: Authentication failure logs not detailed enough, lacking error code classification
  - Fix: Added detailed error code classification handling and statistical information
  - Impact: WebSocket authentication issue troubleshooting

- **AES/SHA256 Resource Leak Fix**
  - Files:
    - `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
    - `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: AES and SHA256 instances not properly released, may cause resource leaks
  - Fix: Use `using` statements to ensure resources are released correctly
  - Impact: All scenarios using event decryption

- **InMemoryFailedEventStore Thread Safety Issue**
  - File: `Mud.Feishu.Webhook/Services/InMemoryFailedEventStore.cs`
  - Issue: Cleanup method not locked, race condition with dictionary operations
  - Fix: Added lock protection in `CleanupExpiredEvents` method
  - Impact: Users of in-memory failed event storage

- **InMemoryFailedEventStore Timer Resource Leak**
  - File: `Mud.Feishu.Webhook/Services/InMemoryFailedEventStore.cs`
  - Issue: Timer instance not released in Dispose method
  - Fix: Implemented IDisposable interface, released Timer in Dispose
  - Impact: Users of in-memory failed event storage

- **FeishuSeqIDDeduplicator Cache Cleanup Logic**
  - File: `Mud.Feishu.Abstractions/Services/FeishuSeqIDDeduplicator.cs`
  - Issue: Maximum SeqID calculation logic incomplete, may cause duplicate processing
  - Fix: Unified calculation of maximum SeqID after cleanup completion
  - Impact: Users of SeqID deduplication

- **Log Sensitive Information Cleanup**
  - File: `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
  - Issue: Logs contain complete decrypted JSON data, may leak sensitive information
  - Fix: Only record event data length, not complete content
  - Impact: All log users

- **Log Emoji Symbol Removal**
  - File: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - Issue: Logs use emoji symbols, may cause log parsing issues in production environments
  - Fix: Removed emoji symbols from logs
  - Impact: Production environment log parsing

- **Nonce Deduplication Logic Comment Optimization**
  - File: `Mud.Feishu.Webhook/Services/FeishuEventValidator.cs`
  - Issue: Code logic correct but lacks clear comments
  - Fix: Added detailed comments explaining return value semantics
  - Impact: Code readability

- **IFeishuEventHandlerFactory Interface Improvement**
  - File: `Mud.Feishu.Abstractions/IFeishuEventHandlerFactory.cs`
  - Issue: Interface missing `UnregisterHandler(IFeishuEventHandler handler)` method definition
  - Fix: Added interface method definition, consistent with implementation class
  - Impact: Users needing to unregister specific event handlers

- **Duplicate Service Registration Fix**
  - File: `Mud.Feishu.Webhook/Extensions/FeishuWebhookServiceBuilder.cs`
  - Issue: `IFeishuNonceDistributedDeduplicator` registered twice
  - Fix: Removed duplicate registration on line 296
  - Impact: All users of Webhook services

### 🐛 Bug Fixes

#### Core Functionality Fixes

- **Background Processing Mode Failed Event Storage**
  - Fixed issue where failed events couldn't be persisted in background processing mode
  - Correctly calls `IFailedEventStore.StoreFailedEventAsync`
  - Added detailed error logging

#### Resource Management Fixes

- **AES Encryption Resource Release**
  - Use `using var` statement to ensure AES instance is released correctly
  - Also release ICryptoTransform resources

- **SHA256 Hash Resource Release**
  - Use `using` statement to ensure SHA256 instance is released correctly

- **Timer Resource Release**
  - InMemoryFailedEventStore implements IDisposable
  - Ensure timed cleanup Timer is released correctly

#### Thread Safety Fixes

- **Failed Event Storage Cleanup Method**
  - Added lock protection for cleanup operations
  - Ensured concurrent safety with dictionary operations

#### Logic Fixes

- **SeqID Deduplication Cleanup**
  - Unified recalculation of maximum SeqID after cleanup completion
  - Avoided calculation errors caused by partial cleanup

#### New Features

- **Redis Fallback Deduplicator**
  - Added `RedisFeishuEventDistributedDeduplicatorWithFallback` class
  - Supports automatic fallback to memory deduplication
  - Exponential backoff retry mechanism
  - Status query and monitoring capabilities

- **WebSocket Error Classification**
  - Distinguish between recoverable and non-recoverable errors
  - Detailed error logs and error type identification
  - Helps locate issues quickly

- **Detailed Authentication Failure Tracking**
  - Classify authentication failure reasons by error code
  - Statistical total failure count and failure time
  - Provide targeted repair suggestions

### 📝 Documentation Improvements

#### New Documentation

- **SECURITY-WARNING.md**
  - Demo project security configuration guide
  - Production environment security checklist
  - Environment variable configuration explanation

#### Improved Documentation

- **README.md**
  - Updated security configuration suggestions
  - Added best practice links

### 🔄 Breaking Changes

#### Configuration Changes

- **Production Environment Signature Validation**
  - Previous: Production environment signature validation could be disabled
  - Now: Production environment forces signature validation, disabling will throw exception

- **Demo Project Configuration**
  - Previous: appsettings.json contained hardcoded example keys
  - Now: Configuration file is empty, must configure through environment variables or manually

### ⚠️ Migration Guide

#### Production Environment Configuration

If you use the following configurations in production environment, you need to make corresponding adjustments:

##### 1. Enable Signature Validation

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true,
    "EnableBodySignatureValidation": true
  }
}
```

##### 2. Configure Environment Variables

```bash
# Linux/macOS
export FeishuWebhook__VerificationToken="your_verification_token"
export FeishuWebhook__EncryptKey="your_32_byte_encryption_key"

# Windows PowerShell
$env:FeishuWebhook__VerificationToken="your_verification_token"
$env:FeishuWebhook__EncryptKey="your_32_byte_encryption_key"
```

##### 3. Verify Configuration

Before deployment, confirm:

- [ ] Signature validation enabled
- [ ] Timestamp tolerance range <= 60 seconds
- [ ] EncryptKey is 32-byte strong key
- [ ] IP validation enabled and whitelist configured
- [ ] Logs don't contain sensitive information

### 🧪 Testing

#### New Tests

- Background processing failed event storage tests
- AES/SHA256 resource release tests
- InMemoryFailedEventStore thread safety tests
- SeqID deduplication cleanup logic tests
- Timer resource release tests
- IFeishuEventHandlerFactory interface method tests
- WebSocket error classification tests
- Redis fallback strategy tests
- Authentication failure handling tests

---

## 1.1.0 (2025-12-31)

**FEATURES**

### 🔧 Core Optimization and Refactoring

- 📦 **Multi-Framework Support**: Supports .NET Standard 2.0, .NET 6.0, .NET 8.0, .NET 10.0
  - Provides cross-platform compatibility, supports from .NET Framework 4.6+ to .NET 10.0
  - Unified API interface, different framework versions use the same programming model
  - Automatic compile-time conditional processing, fully utilize platform features

- 🏗️ **Response Type Unification**: Update all API response types to `FeishuApiResult<T>` series
  - `FeishuApiResult<T>` - Generic response type
  - `FeishuApiPageListResult<T>` - Paginated list response
  - `FeishuApiListResult<T>` - List response
  - `FeishuNullDataApiResult` - Null data response

- 🔧 **Message Sending Interface Refactoring**: Unified message sending interface design
  - `SendMessageRequest` replaces `TextMessageRequest`, supports all message types
  - `MessageTextContent` replaces `TextContent`, maintains type consistency
  - Improved Content field serialization mechanism

### 📋 New Approval API Support

- ✅ **Approval Definition Management**
  - `IFeishuV4Approval` - V4 approval base interface
  - `IFeishuTenantV4Approval` - V4 tenant approval interface
  - Supports creating approval definitions, querying approval instances and other core functions

### 📝 Task Management Enhancement

- 🎯 **Custom Field Management**
  - Create, update, query custom fields
  - Custom field option management
  - Custom field resource binding
  - Paginated query of custom field lists

- 📊 **Task Group Management**
  - Create, update, delete task groups
  - Query task group lists
  - Task group resource binding

### 🔄 WebSocket Real-Time Event Subscription

- 🌐 **Feishu WebSocket Client** (`Mud.Feishu.WebSocket`)
  - Supports Feishu WebSocket real-time event subscription
  - Automatic reconnection mechanism, ensures connection stability
  - Heartbeat detection, promptly discovers connection anomalies
  - Binary message parsing, supports complete event types

### 📡 Webhook Event Processing

- 🎭 **Event Handler Abstraction Layer** (`Mud.Feishu.Abstractions`)
  - Strategy pattern architecture, flexibly extend event handlers
  - Factory pattern management, automatically discover and register handlers
  - Complete Feishu event type coverage:
    - User events: Creation, update, departure, status change
    - Department events: Creation, update, deletion
    - Employee events: Onboarding, departure, information change
    - Message events: Reception, send status, read status
    - Task events: Creation, update, deletion, status change
    - Approval events: Submission, approval, rejection, revocation

### 🎨 Message and Card Enhancement

- 📰 **Message Stream Card API**
  - Complete interface support for application message stream cards
  - Card entity component management
  - Card content update and deletion

- 💬 **Group Function Enhancement**
  - Group announcement management
  - Session tab management
  - Group custom menu setting

### 🛠️ Configuration and Tool Optimization

- ⚙️ **Configuration Enhancement**
  - Added `FeishuOptions` configuration class
  - Supports configuration file binding
  - Logging configuration options

- 🔒 **Security Enhancement**
  - URL verification function, prevents malicious requests
  - Authorization header constants unified management

**REFACTOR**

- 📁 **Namespace Unification**
  - Unified interface namespace structure
  - Global reference imports, simplified code

- 🧹 **Code Cleanup**
  - Removed obsolete TaskSectionsResult class
  - Removed invalid classes and interfaces
  - Unified service registration API, removed obsolete `UseMultiHandler` method

**BUG FIX**

- 🔧 **Fixed HttpClient configuration issues**
- 🔧 **Fixed API endpoint URL format issues**
- 🔧 **Fixed message and task attachment API implementations**

**DOCS**

- 📚 **Documentation Updates**
  - Updated README document structure
  - Removed redundant architecture design and performance characteristics documentation
  - Added department event handler documentation and usage examples
  - Optimized project description and feature explanation

---

## 1.0.2 (2025-11-26)

**FEATURES**

- 🏗️ **Refactoring Optimization**: Created `ChatGroupBase` base class, integrating chat group related common properties
  - Reduced 70+ duplicate properties, improved code reusability
  - Unified `GetChatGroupInfoResult`, `CreateUpdateChatResult`, `UpdateChatRequest`, `CreateChatRequest` class structure
  - Maintained complete JsonPropertyName attributes, ensured JSON serialization compatibility

- 📚 **Documentation Improvement**: Added complete XML documentation comments to all chat group and group member related classes
  - `ChatGroupModeratorPageListResult` - Chat group moderator paginated list result
  - `ChatItemInfo` - Basic chat item information
  - `ShareLinkDataResult` - Share link data result
  - `AddMemberResult` - Add member operation result
  - `GetMemberIsInChatResult` - Member group status query result
  - `GetMemberPageListResult` - Group member paginated list result
  - `RemoveMemberResult` - Remove member operation result
  - `GroupManagerResult` - Group manager operation result

- 🎯 **Code Quality**: Improved code readability and maintainability
  - All new comments follow C# XML documentation specification
  - Contains detailed business meaning and usage scenario explanations
  - Distinguishes actual effects of different parameter values

## 1.0.1 (2025-11-20)

**REFACTOR**

- Optimized dependency injection configuration structure
- Improved concurrency safety of token managers
- Refactored HTTP client factory configuration

**FEATURES**

- Enhanced error handling mechanisms
- Added detailed logging support
- Supports custom HTTP header configuration

**BUG FIX**

- Fixed concurrency issues during token refresh
- Resolved data loss issues in pagination queries
- Fixed status tracking errors in batch message sending

### 📱 Message Service

- **Multiple Message Types**: Rich message types including text, images, files, cards, etc.
- **Batch Sending**: Supports batch message sending and status tracking
- **Message Interaction**: Emojis reply, message recall, read receipt
- **Asynchronous Processing**: Fully asynchronous message processing mechanism

---

## 1.0.0 (2025-11-01)

### 🎉 First Release - Mud.Feishu Feishu API SDK

**FEATURES**

### 🔐 Authentication and Authorization System

- **Multiple Token Management**: Supports application tokens, tenant tokens, user tokens
- **Automatic Refresh Mechanism**: Intelligent token refresh, triggered 5 minutes in advance
- **High Concurrency Security**: Uses `ConcurrentDictionary` and `Lazy<Task>` to avoid cache breakdown
- **OAuth Authorization Process**: Complete support for Feishu OAuth 2.0 authorization

### 🏢 Organizational Structure Management

#### User Management (V1/V3)

- **User CRUD**: Create, query, update, delete users
- **Batch Operations**: Batch get user information, batch status updates
- **Department Association**: Many-to-many relationship management between users and departments
- **Search Filtering**: Supports various search criteria and pagination

#### Department Management (V1/V3)

- **Tree Structure**: Supports infinite level department tree
- **Recursive Query**: Recursively get sub-departments and members
- **Permission Inheritance**: Automatic department permission inheritance mechanism

#### Employee Management (V1)

- **Employee Information**: Employee detailed information management
- **Onboarding and Departure**: Employee onboarding and departure process support

#### User Group Management (V3)

- **User Group CRUD**: Create, query, update, delete user groups
- **Member Management**: Adding, removing, querying user group members
- **Permission Allocation**: Permission control based on user groups

### 🏢 Enterprise Management System

#### Personnel Type Management (V3)

- **Classification System**: Employee type classification and label management
- **Flexible Configuration**: Supports custom personnel type attributes

#### Job Level Management (V3)

- **Job Level System**: Complete job level promotion and management
- **Job Level Association**: Link with salary, permissions

#### Job Title Management (V3)

- **Career Path**: Employee career development path management
- **Sequence Definition**: Different sequence position definitions

#### Position Management (V3)

- **Position Definition**: Specific position responsibilities and authority definition
- **Position Assignment**: Employee position allocation and changes

#### Role Management (V3)

- **Permission Roles**: Role-Based Access Control (RBAC)
- **Role Inheritance**: Role permission inheritance and combination
- **Member Management**: Adding, removing role members operations

#### Unit Management (V3)

- **Organizational Units**: Enterprise organizational unit management
- **Unit Hierarchy**: Hierarchy between units

#### Work City Management (V3)

- **Office Locations**: Work city and location management
- **Location Association**: Association with departments, employees

### 🔧 Core Technical Features

#### Attribute-Driven Design

- **[HttpClientApi] Attribute**: Automatically generate HTTP client code
- **Strong Type Support**: Compile-time type checking, reduce runtime errors
- **Unified Response**: Unified response handling based on `FeishuApiResult<T>`

#### Dependency Injection Friendly

- **Service Registration**: `AddFeishuApiService()` extension method
- **Flexible Configuration**: Supports configuration files and code configuration
- **Lifecycle Management**: Automatic service lifecycle management

#### High-Performance Caching

- **Smart Caching**: Automatic token caching and refresh
- **Concurrency Control**: Solve cache issues under high concurrency
- **Resource Management**: Implements `IDisposable` interface

#### Exception Handling

- **Unified Exception**: `FeishuException` unified exception handling
- **Error Classification**: Classification handling of different error types
- **Log Integration**: Integrated with .NET log system

### 🌐 API Coverage

#### Authentication and Authorization API

- `IFeishuV3AuthenticationApi` - V3 authentication and authorization interface

#### Message Service API

- `IFeishuV1Message` - V1 message base interface
- `IFeishuTenantV1Message` - V1 tenant message interface
- `IFeishuUserV1Message` - V1 user message interface
- `IFeishuTenantV1BatchMessage` - V1 batch message interface

#### Organizational Structure API (V1)

- `IFeishuV1ChatGroup` - V1 chat group base interface
- `IFeishuTenantV1ChatGroup` - V1 tenant chat group interface
- `IFeishuUserV1ChatGroup` - V1 user chat group interface
- `IFeishuV1ChatGroupMember` - V1 chat group member base interface
- `IFeishuTenantV1ChatGroupMember` - V1 tenant chat group member interface
- `IFeishuUserV1ChatGroupMember` - V1 user chat group member interface
- `IFeishuV1Departments` - V1 department management base interface
- `IFeishuTenantV1Departments` - V1 tenant department management interface
- `IFeishuUserV1Departments` - V1 user department management interface
- `IFeishuV1Employees` - V1 employee management base interface
- `IFeishuTenantV1Employees` - V1 tenant employee management interface
- `IFeishuUserV1Employees` - V1 user employee management interface

#### Enterprise Management API (V3)

- `IFeishuV3Departments` - V3 department management base interface
- `IFeishuTenantV3Departments` - V3 tenant department management interface
- `IFeishuUserV3Departments` - V3 user department management interface
- `IFeishuTenantV3EmployeeType` - V3 tenant personnel type management interface
- `IFeishuTenantV3JobFamilies` - V3 tenant job family management interface
- `IFeishuTenantV3JobLevel` - V3 tenant job level management interface
- `IFeishuV3JobTitle` - V3 position management base interface
- `IFeishuTenantV3JobTitle` - V3 tenant position management interface
- `IFeishuUserV3JobTitle` - V3 user position management interface
- `IFeishuTenantV3RoleMember` - V3 tenant role member management interface
- `IFeishuTenantV3Role` - V3 tenant role management interface
- `IFeishuTenantV3Unit` - V3 tenant unit management interface
- `IFeishuV3User` - V3 user management base interface
- `IFeishuTenantV3User` - V3 tenant user management interface
- `IFeishuUserV3User` - V3 user management interface
- `IFeishuTenantV3UserGroupMember` - V3 tenant user group member management interface
- `IFeishuTenantV3UserGroup` - V3 tenant user group management interface
- `IFeishuTenantV3WorkCity` - V3 tenant work city management interface
- `IFeishuV3WorkCity` - V3 work city base interface

### 📦 Technology Stack

#### Framework Support

- **.NET Standard 2.0** - Compatible with .NET Framework 4.6.1+
- **.NET 6.0** - LTS Long-term support version
- **.NET 8.0** - LTS Long-term support version
- **.NET 10.0** - LTS Long-term support version

#### Core Dependencies

- **Mud.ServiceCodeGenerator v1.4.5.3** - HTTP client code generator
- **System.Text.Json v10.0.1** - High-performance JSON serialization (.NET Standard 2.0)
- **Microsoft.Extensions.Http** - HTTP client factory
  - .NET 6.0 / .NET Standard 2.0: v8.0.1
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Http.Polly** - Resilience and transient fault handling
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Logging** - Logging
  - .NET 6.0 / .NET Standard 2.0: v8.0.3
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Configuration.Binder** - Configuration binding
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1

## 🔗 Related Links

- [Project Gitee Homepage](https://gitee.com/mudtools/MudFeishu)
- [Project Github Homepage](https://github.com/mudtools/MudFeishu)
- [NuGet Package](https://www.nuget.org/packages/Mud.Feishu/)
- [Documentation Site](https://www.mudtools.cn/documents/guides/feishu/)
- [Feishu Open Platform](https://open.feishu.cn/document/)
- [Issue Feedback](https://gitee.com/mudtools/MudFeishu/issues)

---

_Note: This CHANGELOG follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) specification._

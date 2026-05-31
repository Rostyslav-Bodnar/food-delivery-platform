# Food Delivery Platform

A production-style, full-stack food delivery platform built as a polyglot persistence, microservices system. Customers browse restaurants, place and pay for orders; restaurants manage menus and fulfill orders; couriers accept deliveries, broadcast live GPS, and get paid out through Stripe Connect. Everything is glued together by a custom API Gateway, an event-driven RabbitMQ bus, RPC over RabbitMQ for synchronous lookups, SignalR for real-time tracking, and a per-service GitHub Actions → GHCR → Render pipeline.

---

## Table of contents

1. [Repository layout](#repository-layout)
2. [High-level architecture](#high-level-architecture)
3. [Feature catalog](#feature-catalog)
4. [HTTP API surface](#http-api-surface)
5. [Frontend](#frontend)
6. [API Gateway](#api-gateway)
7. [Shared Contracts (DF.Contracts)](#shared-contracts-dfcontracts)
8. [Microservices](#microservices)
   * [UserService](#userservice)
   * [MenuService](#menuservice)
   * [OrderService](#orderservice)
   * [PaymentService](#paymentservice)
   * [TrackingService](#trackingservice)
   * [PromoService](#promoservice)
9. [Service-to-service communication](#service-to-service-communication)
10. [Real-time (SignalR)](#real-time-signalr)
11. [Authentication & internal security](#authentication--internal-security)
12. [Payments & payouts (Stripe)](#payments--payouts-stripe)
13. [Persistence](#persistence)
14. [Observability](#observability)
15. [Resilience & reliability patterns](#resilience--reliability-patterns)
16. [Testing](#testing)
17. [CI/CD](#cicd)
18. [Deployment topology](#deployment-topology)
19. [Local development](#local-development)
20. [Environment & configuration](#environment--configuration)
21. [Third-party integrations](#third-party-integrations)

---

## Repository layout

```
food-delivery-platform/
├── .github/workflows/        # Per-service GitHub Actions pipelines
│   ├── frontend.yml
│   ├── gateway.yml
│   ├── userservice.yml
│   ├── menuservice.yml
│   ├── orderservice.yml
│   ├── paymentservice.yml
│   └── trackingservice.yml
├── backend/
│   ├── Contracts/DF.Contracts/      # Shared DTOs / events / RPC contracts (NuGet)
│   ├── Gateway/DF.Gateway.API/      # Reverse-proxy + auth + HMAC signer
│   ├── UserService/                 # Identity, accounts, Stripe Connect onboarding
│   ├── MenuService/                 # Restaurants, dishes, ingredients
│   ├── OrderService/                # Order lifecycle, outbox, idempotency
│   ├── PaymentService/              # Stripe payments, refunds, courier payouts
│   ├── TrackingService/             # Live GPS, SignalR hub, PostGIS
│   ├── PromoService/                # (scaffolded)
│   └── global.json
├── frontend/food-delivery-platform/ # React 19 + Vite SPA
├── compose.yaml                     # Full stack in Docker
├── compose.dev-infra.yaml           # Infra-only for hybrid local dev
└── README.md
```

Each backend service follows a clean-architecture project layout:

```
<Service>/
├── DF.<Service>.API/             # ASP.NET host: controllers, middleware, Program.cs, Dockerfile
├── DF.<Service>.Application/     # services, repositories (interfaces+impls), messaging, validators
├── DF.<Service>.Domain/          # entities, value objects, enums
├── DF.<Service>.Infrastructure/  # EF Core DbContext, migrations, third-party adapters
├── DF.<Service>.Contracts/       # service-local DTOs / response models
└── <Service>.sln
```

---

## High-level architecture

```
                     ┌──────────────────────────────┐
                     │   React 19 SPA (Vercel)      │
                     │   axios + SignalR + Stripe   │
                     └──────────────┬───────────────┘
                                    │ HTTPS, JWT (cookie + bearer)
                                    ▼
                     ┌──────────────────────────────┐
                     │     DF.Gateway.API           │
                     │  JWT validation, CORS, HMAC  │
                     │  request signer, Polly retry │
                     └──┬──────┬──────┬──────┬──────┘
                        │      │      │      │
        ┌───────────────┘      │      │      └────────────────┐
        ▼                      ▼      ▼                       ▼
   UserService            MenuService OrderService       PaymentService
   (PG + Identity)        (PG+Redis)  (PG, outbox)       (PG, outbox, Stripe)
        │                      │      │                       │
        └──────┐         ┌─────┴──────┴────┐         ┌────────┘
               ▼         ▼                 ▼         ▼
              ┌──────────────────────────────────────┐
              │           RabbitMQ broker            │
              │   pub/sub (df.events) + RPC queues   │
              └──────────────────────────────────────┘
                              │
                              ▼
                      TrackingService
                      (PostGIS + Redis + SignalR Hub)
```

Three planes of communication:

| Plane | Transport | Used for |
| --- | --- | --- |
| **Sync edge** | HTTPS via Gateway | Client → server CRUD, requests with user context |
| **Sync internal** | RabbitMQ RPC (direct reply-to) | Per-request lookups (e.g. MenuService → UserService for the restaurant that owns a dish) |
| **Async events** | RabbitMQ topic exchange `df.events` | Order lifecycle fan-out (created → picked up → delivered → paid out), Stripe payouts, location updates |
| **Real-time client** | SignalR (WebSocket) on TrackingService | Live courier GPS, order-status transitions, tracking-stage updates |

---

## Feature catalog

Every implemented capability, grouped by user role and by cross-cutting platform concern. Each line is a real feature backed by the current code — endpoint(s), background worker, or UI component.

### Account & identity

| # | Feature | Implementation |
| --- | --- | --- |
| 1 | Email + password registration | `POST /api/auth/register` (UserService); ASP.NET Identity `UserManager`; auto-creates a `CustomerAccount` so every new user can shop immediately |
| 2 | Login with JWT access + refresh tokens | `POST /api/auth/login`; refresh token issued as **HttpOnly** cookie, access token in body |
| 3 | Silent refresh | `POST /api/auth/refresh` rotates both tokens; cookie is replayed by the browser |
| 4 | Logout with refresh revocation | `POST /api/auth/logout` revokes the `RefreshToken` row and clears the cookie |
| 5 | "Me" profile lookup | `GET /api/user/profile` resolves the caller from `IUserContext` |
| 6 | Multi-account per user (Customer / Business / Courier) | `Account` entity hierarchy with TPH discriminator; `User.AccountId` points at the active one |
| 7 | Create a new role-specific account | `POST /api/account/customer\|business\|courier` (multipart for avatar/license uploads) |
| 8 | Update each account type | `PUT /api/account/customer\|business\|courier` (multipart) |
| 9 | Delete account | `DELETE /api/account/{id}` |
| 10 | Switch active account (re-issue JWT with new claims) | `PUT /api/profile/switch/{accountId}` — generates a fresh JWT carrying the new `account_id` + `account_type` |
| 11 | Profile page bundle (user + active + all accounts) | `GET /api/profile` |
| 12 | List business accounts (catalog) | `GET /api/account/all/business` |
| 13 | Avatar / business logo / courier license image upload | `CloudinaryService` (UserService) with `IFormFile` |
| 14 | Phone number, full name, surname, address per account | persisted on the account entity |
| 15 | Multiple delivery addresses per customer | `BusinessLocation` + `Location` in TrackingService, linked back on the customer profile |

### Restaurants & menu (Business + Customer)

| # | Feature | Implementation |
| --- | --- | --- |
| 16 | Restaurant catalog with search + sort + category filter | Customer home `CustomerHomePage` + `RestaurantsFilter` (lucide-react `Search` / `Filter`) |
| 17 | Restaurant detail page with sticky header | `RestaurantDetailsPage` + `StickyHeader` |
| 18 | Dish browsing — paginated, server-side | `GET /api/dish/customer` returns items + `X-Pagination-*` headers (Page/PageSize/TotalCount/TotalPages) |
| 19 | Dish detail view | `GET /api/dish/customer/{id}`; dual-column layout (`DishLeftColumn` / `DishRightColumn`) |
| 20 | Dishes by restaurant | `GET /api/dish/customer/{businessId}/dish` (paginated) |
| 21 | Hot dish caching | Redis (`StackExchange.Redis`) inside MenuService |
| 22 | Business-side dish CRUD | `POST /api/dish/create` (multipart + idempotency), `POST /api/dish/update`, `DELETE /api/dish/{id}` |
| 23 | Ingredient management | `IngredientService` + `IIngredientRepository` in MenuService |
| 24 | Dish image upload | `CloudinaryService` in MenuService (separate Cloudinary client from UserService's) |
| 25 | 15 built-in dish categories | `Category` enum (Drink, Soup, Salad, Pizza, Burger, Pasta, Sushi, Dessert, Breakfast, Grill, SideDish, Sauce, Vegan, KidsMenu, SpecialOffer); served by `GET /api/category` |
| 26 | Business home dish manager with delete confirmation modal | `BusinessHomePage` + `DishesContent` + `DeleteConfirm` |
| 27 | Idempotent dish creation | `[Idempotent]` filter + `IdempotencyKey` row |

### Cart & checkout (Customer)

| # | Feature | Implementation |
| --- | --- | --- |
| 28 | Local cart | `CartPage` + `cart` feature hooks |
| 29 | Multi-restaurant cart split into per-restaurant orders | `CheckoutPage` builds one `CreateOrderRequest` per restaurant (`RestaurantSection`, `RestaurantPaymentCard`) |
| 30 | Delivery vs. pickup choice | `DeliveryMethod` enum (`Delivery`, `Pickup`) on the order |
| 31 | Online vs. cash-on-delivery payment choice | `PaymentMethod` enum (`Online`, `CashOnDelivery`) |
| 32 | Delivery address entry | `DeliveryAddress` component |
| 33 | Customer contact info on checkout | `ContactInfo` component |
| 34 | Free-text comment per order | `CommentSection` component |
| 35 | Animated checkout backdrop | `ParticlesBackground` (framer-motion) |
| 36 | Order total with breakdown (subtotal / delivery fee / grand total) | `SummaryBlock` + `TotalSection` |
| 37 | Embedded Stripe Elements payment modal | `StripePaymentModal` + `CardForm` (`@stripe/react-stripe-js`) |
| 38 | Server-side idempotent order creation | `POST /api/order/create` and `POST /api/order/create/batch` both `[Idempotent]` |

### Order lifecycle

| # | Feature | Implementation |
| --- | --- | --- |
| 39 | Six-state status machine | `OrderStatus` enum: Canceled, Preparing, Ready, OutForDelivery, PickedUp, Delivered |
| 40 | Business transitions (Accept → Preparing → Ready) | `PATCH /api/order/status?orderId=&status=` + business UI `StatusActions` |
| 41 | Courier accepts a Ready order | flips status to OutForDelivery via the same endpoint |
| 42 | Courier marks pickup | `UpdateTrackingStage` hub call with stage `pickup`, plus order status PickedUp |
| 43 | Courier marks delivery | `POST /api/order/courier/deliver?orderId=&courierId=` |
| 44 | Cash-on-delivery settlement | `PATCH /api/order/courier/mark-paid?orderId=&courierId=` (settles ledger, fires `OrderCourierPaidEvent`) |
| 45 | Customer or business cancellation | `PATCH /api/order/cancel?orderId=` |
| 46 | Order details modal | `OrderDetailsComponent` with sub-cards: client, courier, status banner, items, delivery, payment |
| 47 | Customer active orders list | `CustomerOrdersPage` + `GET /api/order/customer/{customerId}` |
| 48 | Business order queue with status filter + selection | `BusinessOrdersPage` + `useBusinessOrders` / `useOrderFilter` / `useOrderSelection` / `useOrderStatus` |
| 49 | Courier inbox (Ready/Available, Active) | `CourierOrdersPage` + `ActiveOrderSection` + `NewOrdersSection` |
| 50 | Per-role history pages | `CustomerOrderHistoryPage`, `BusinessOrderHistoryPage`, `CourierOrderHistoryPage` + `GET /api/order/{role}/{id}/history` |
| 51 | Paginated "all orders" admin read | `GET /api/order/all?page=&pageSize=` |
| 52 | Order details fetch | `GET /api/order/details/{orderId:guid}` |
| 53 | Active courier orders | `GET /api/order/courier/active?courierId=` |
| 54 | Transactional outbox | `OutboxMessage` table + `OutboxPublisherHostedService` writes to RabbitMQ atomically with the state change |
| 55 | Order-payment timeout | `OrderPaymentTimeoutWorker` cancels orders whose payment never settles |

### Real-time order tracking

| # | Feature | Implementation |
| --- | --- | --- |
| 56 | Per-order tracking-token JWT issuance | `POST /api/orders/{orderId}/tracking-token` (OrderService) returns a JWT scoped to the order, with a `tracking:write` scope for couriers |
| 57 | SignalR hub authentication via main JWT **or** tracking token | `CourierTrackingHub` `[Authorize(AuthenticationSchemes = "TrackingHub,Bearer")]` |
| 58 | User-scoped subscriptions (per role) | `SubscribeToUserOrders()` joins `customer:{id}` / `business:{id}` / `courier:{id}` |
| 59 | Couriers also auto-subscribe to "available orders" feed | extra `couriers:available` group on subscribe |
| 60 | Per-order subscription with initial snapshot replay | `SubscribeToOrder(orderId)` returns the cached `OrderTrackingSnapshotDto` |
| 61 | Courier streams GPS | `SendLocation(CourierLocationDto)` with order-id claim binding |
| 62 | Tracking-stage transitions (awaiting-courier → to-business → to-customer) | `UpdateTrackingStage`; refuses skipping `to-customer` before pickup is confirmed |
| 63 | Server fans out `TrackingSnapshotUpdated`, `CourierLocationUpdated`, `OrderStatusUpdated` | `BroadcastSnapshotAsync` to the order group |
| 64 | Snapshot persistence | `OrderTrackingSnapshotStore` (Redis) |
| 65 | Customer live tracking modal with route + courier marker | `LiveOrderTrackingModal` (`react-leaflet`) |
| 66 | ETA calculation + auto-recalc | `useCourierEta` hook (recomputes every 30 s along the live route) |
| 67 | Courier-side location streamer | `useCourierLocationSender` (browser Geolocation API → hub `SendLocation`) |
| 68 | Courier route map (business → customer) | `CourierRouteMap` |
| 69 | Server-side OSRM driving route | `RoutingService` (calls OSRM `route/v1/driving`, returns distance/duration/GeoJSON) |
| 70 | Forward & reverse geocoding | `GeolocationService` (calls `geocode.maps.co`) |
| 71 | Order-events subscription hook | `useOrderEventsSubscription` keeps lists fresh as statuses change |
| 72 | Status broadcasts driven by `OrderStatusChangedEvent` | `OrderStatusChangedConsumer` in TrackingService pushes to all relevant groups |
| 73 | Rate-limited hub + REST surface | ASP.NET RateLimiter (sliding window) |

### Locations (TrackingService)

| # | Feature | Implementation |
| --- | --- | --- |
| 74 | Create / list / get / update / delete address | `LocationController` CRUD (`POST/GET/PUT/DELETE /api/location`) with pagination (`skip`/`take` capped at 500) |
| 75 | Bulk "add location" endpoint | `POST /api/location/add` |
| 76 | Business location CRUD | `BusinessLocationController` (`POST/GET/DELETE /api/businesslocation`) |
| 77 | List locations for a business | `GET /api/businesslocation/business/{businessId}` |
| 78 | Spatial storage | PostGIS-backed Postgres |
| 79 | RPC fetch of business locations from UserService | `GetBusinessLocationConsumer` + `GetBusinessLocationBatchConsumer` |

### Payments (PaymentService)

| # | Feature | Implementation |
| --- | --- | --- |
| 80 | Read payment by order id | `GET /api/payments/{orderId}` (status, method, `client_secret`) |
| 81 | Online payment via Stripe PaymentIntent | `CreateStripePaymentIntentCommandHandler` — creates PI with `application_fee_amount` + `transfer_data.destination` to the business connected account |
| 82 | Cash-on-delivery flow | `CollectCashCommandHandler` + `POST /api/payments/{paymentId}/cash/collect` |
| 83 | Cancel a pending payment | `POST /api/payments/{paymentId}/cancel` (Stripe `PaymentIntent.cancel` for Online; CoD path just cancels the expectation) |
| 84 | Full or partial refund | `POST /api/payments/{paymentId}/refunds` with optional `Idempotency-Key` header |
| 85 | Payment history (incl. refunds list) | `GET /api/payments/{paymentId}/history` |
| 86 | Stripe webhook ingestion | `POST /webhooks/stripe/main` — signature verified, idempotency via `ProcessedWebhook` table |
| 87 | Handled webhook events | `payment_intent.succeeded`, `payment_intent.payment_failed`, `payment_intent.canceled`, `refund.succeeded`, `charge.refunded`, `charge.dispute.created`, `charge.dispute.updated`, `charge.dispute.closed` |
| 88 | Outbox for outbound payment events | `OutboxPublisher` (50 batch, 2 s poll, 5 retries, exp back-off, DLQ via `OutboxDeadMessage`) |
| 89 | Stripe out-of-band processing | `StripeTaskProcessor` (50 batch, 5 s poll, 5 retries, 15 min payment expiration window) |
| 90 | Auto-cancel timed-out payments | `PaymentTimeoutWorker` (1 min poll, 10 s safety window) |
| 91 | Payment provisioned automatically on `OrderCreatedEvent` | `OrderCreatedConsumer` |
| 92 | Refund triggered on `OrderCancelledEvent` | `OrderCancelledConsumer` |
| 93 | Capture / settle on `OrderDeliveredEvent` | `OrderDeliveredConsumer` |
| 94 | Internal `PaymentSucceededConsumer` notifies OrderService | order is marked paid |

### Courier payouts (PaymentService + UserService)

| # | Feature | Implementation |
| --- | --- | --- |
| 95 | Per-order courier earning ledger | `CourierEarning` + `CourierEarningStatus` |
| 96 | Aggregated courier balance (pending + available) | `CourierBalance` |
| 97 | Read balance | `GET /api/courier-payouts/{courierId}/balance` |
| 98 | Link a courier's Stripe Connect id | `PUT /api/courier-payouts/{courierId}/account` |
| 99 | Scheduled payouts to courier Stripe accounts | `CourierPayoutWorker` (settings in `CourierPayouts:*`) |
| 100 | Payout success event | `CourierPayoutCompletedEvent` published; OrderService consumes for ledger close-out |
| 101 | UserService Stripe payout webhooks | `POST /webhooks/stripe/payout` — handles `payout.created`, `payout.paid`, `payout.failed`, persists `PayoutRecord` |

### Stripe Connect (business onboarding)

| # | Feature | Implementation |
| --- | --- | --- |
| 102 | Background provisioning of Stripe Connect accounts | `StripeAccountProvisioningWorker` walks pending businesses, creates Connect accounts |
| 103 | On-demand onboarding link | `GET /api/account/onboarding/{businessId}` — returns either a Stripe AccountLink or `202 Accepted` + `Retry-After` if provisioning is still in flight |
| 104 | Onboarding completion + refresh return URLs | SPA pages `/stripe/onboarding/done` and `/stripe/onboarding/refresh` |
| 105 | Connect account.updated webhook | `POST /webhooks/stripe/connect` syncs business's `payouts_enabled`, `charges_enabled`, etc. |
| 106 | Webhook idempotency | `IProcessedWebhookStore` + `ProcessedWebhook` table per webhook controller |

### Business dashboard

| # | Feature | Implementation |
| --- | --- | --- |
| 107 | KPI grid (revenue, orders, AOV, etc.) | `KpiGrid` + `BusinessDashboardService.GetDashboardAsync` |
| 108 | Daily income + outcome breakdown charts | `DashboardCharts` (recharts) |
| 109 | Range presets — 7 / 30 / 90 / 365 days | `RangeToggles` |
| 110 | Payout history table | `PayoutsTable` (joins `PayoutRecord` per business) |
| 111 | Manual on-demand payout drain | `POST /api/account/business/{id}/payouts/manual` + `ManualPayoutButton` — fires `payout.created` on Stripe |
| 112 | Revenue by dish | `GET /api/order/business/{id}/revenue-by-dish?from=&to=` (default last 30 days, UTC) |
| 113 | Stripe-live reads (no local aggregation table) | Dashboard reads fresh from the business's Connect account each call |

### Role-specific UI shells

| # | Feature | Implementation |
| --- | --- | --- |
| 114 | Customer home (search hero + filter sidebar + popular + all dishes) | `CustomerHomePage`, `SearchHero`, `FilterSidebar`, `PopularSection`, `AllDishesSection` |
| 115 | Business home (header + dish manager + footer) | `BusinessHomePage`, `BusinessHeader`, `DishesContent`, `BusinessFooter` |
| 116 | Courier home (active order + new orders + history sections) | `CourierHomePage`, `ActiveOrderSection`, `NewOrdersSection`, `HistorySection` |
| 117 | Role-aware sidebars | `CustomerSidebar`, `BusinessSidebar`, `CourierSidebar`, `RoleSidebar` dispatcher |
| 118 | Unauthenticated home variant | `UnauthenticatedHome` |
| 119 | Protected routing | `ProtectedRoute` wrapper |
| 120 | Toast service for backend errors / successes | `ToastService` + axios response interceptor flag `skipErrorToast` |
| 121 | Global error boundary | `global-components/error/` |
| 122 | Aborted-request suppression | axios interceptor ignores cancelled / aborted requests caused by React StrictMode double-mount and route navigation |

### Platform & infrastructure features

| # | Feature | Implementation |
| --- | --- | --- |
| 123 | Custom reverse-proxy gateway (not Ocelot/YARP) | `GatewayProxy` + `ServiceResolver` + `[GatewayService]` attribute |
| 124 | HMAC-signed internal calls | `InternalAuthSigner` + `InternalAuthMiddleware` in every service |
| 125 | Replay protection on internal calls | 5-minute timestamp window in `InternalAuthMiddleware` |
| 126 | Timing-safe API key compare | `CryptographicOperations.FixedTimeEquals` |
| 127 | User-context propagation | `X-Internal-UserId/Role/AccountId/AccountType` headers + `UserContextMiddleware` |
| 128 | Polly retry on transient downstream failures | Gateway HttpClient policy (3 attempts, exp back-off, GET/HEAD/OPTIONS only) |
| 129 | Resilient RabbitMQ connect | Polly retry on initial connect (8 attempts, exp, jittered) + auto-recovery + topology recovery |
| 130 | RPC over RabbitMQ (direct reply-to) | `RpcChannel`, `RpcConsumerBase`, per-service clients |
| 131 | Async event bus | RabbitMQ topic exchange `df.events` |
| 132 | Transactional outbox + dead-letter promotion | OrderService and PaymentService |
| 133 | Processed-message store (deduplication for inbound events) | `ProcessedMessage` table |
| 134 | Webhook idempotency tables | per service (`ProcessedWebhook`) |
| 135 | EF Core migrations applied on startup | `db.Database.MigrateAsync()` in every Program.cs |
| 136 | `--migrate` CLI mode for migrator containers | invoked by per-service `*.migrator` services in `compose.yaml` |
| 137 | OpenTelemetry traces + metrics | ASP.NET, HttpClient, EF instrumentation; OTLP export gated on `OTEL_EXPORTER_OTLP_ENDPOINT` |
| 138 | Serilog structured JSON | Compact JSON formatter + request logging |
| 139 | Three-tier health checks | `/health`, `/health/live`, `/health/ready` (Postgres + RabbitMQ tagged checks) |
| 140 | Server-side pagination | `PageRequest` + `X-Pagination-*` response headers |
| 141 | FluentValidation auto-validation | MenuService + OrderService (via `SharpGrip.FluentValidation.AutoValidation.Mvc`) |
| 142 | Unified `Response<T>` envelope | every Gateway-proxied response |
| 143 | Global exception middleware | maps domain exceptions to HTTP codes, returns `ServiceErrorResponse` |
| 144 | OpenAPI / Swagger | UserService + MenuService + others (`AddSwaggerGen` / `AddOpenApi`) |
| 145 | CI per service with path-filtered triggers | seven workflows under `.github/workflows/` |
| 146 | Multi-stage Docker images per service | each `DF.*.API/Dockerfile` |
| 147 | GHCR container publishing tagged `:latest` + `:<sha>` | `docker/build-push-action@v6` with GHA cache |
| 148 | Render deploy hooks per service | `curl -X POST $RENDER_DEPLOY_HOOK` step |
| 149 | Frontend deploys to Vercel + `gh-pages` backup | `frontend.yml` (artifact upload + `peaceiris/actions-gh-pages`) |
| 150 | `DF.Contracts` NuGet package on GitHub Packages | shared DTOs/events/RPC envelopes, consumed by every service |

---

## HTTP API surface

Every public endpoint goes through the Gateway at `https://<gateway>/api/...`. Below is the full surface, grouped by the downstream service that ultimately handles the request.

### Gateway → UserService

```
POST   /api/auth/register                          register (creates Customer account)
POST   /api/auth/login                             login
POST   /api/auth/refresh                           rotate access + refresh tokens
POST   /api/auth/logout                            revoke refresh token
GET    /api/user/profile                           current user
GET    /api/user/user?userId={id}                  any user by id
GET    /api/user/users                             all users
GET    /api/profile                                profile bundle (user + accounts)
PUT    /api/profile/switch/{accountId}             switch active account, re-issue JWT
GET    /api/account/{userId}                       account by user id
GET    /api/account/all/{userId}                   all accounts for a user
GET    /api/account/all/business                   list business accounts
POST   /api/account/customer                       create customer account (multipart)
POST   /api/account/business                       create business account (multipart)
POST   /api/account/courier                        create courier account (multipart)
PUT    /api/account/customer|business|courier      update account (multipart)
DELETE /api/account/{id}                           delete account
GET    /api/account/onboarding/{businessId}        Stripe Connect onboarding link (202 if pending)
GET    /api/account/business/{id}/dashboard        KPI + chart + payout data
POST   /api/account/business/{id}/payouts/manual   on-demand payout drain
POST   /webhooks/stripe/connect                    Stripe Connect webhook
POST   /webhooks/stripe/payout                     Stripe payout webhook
```

### Gateway → MenuService

```
GET    /api/category                               built-in category enum
GET    /api/dish?page=&pageSize=                   all dishes (paginated, X-Pagination-* headers)
GET    /api/dish/{id}                              one dish
GET    /api/dish/customer?page=&pageSize=          customer-facing dish list
GET    /api/dish/customer/{id}                     customer-facing dish detail
GET    /api/dish/customer/{businessId}/dish        dishes for a restaurant (customer)
GET    /api/dish/{businessId}/dish                 dishes for a restaurant (business)
POST   /api/dish/create                            create dish (multipart, idempotent)
POST   /api/dish/update                            update dish (multipart)
DELETE /api/dish/{id}                              delete dish
```

### Gateway → OrderService

```
GET    /api/order/all?page=&pageSize=              paginated all-orders
GET    /api/order/details/{orderId}                full order detail
GET    /api/order/business?businessId=             open orders for a business
GET    /api/order/courier?courierId=               orders for a courier
GET    /api/order/courier/active?courierId=        active orders for a courier
GET    /api/order/customer/{customerId}            customer's active orders
GET    /api/order/customer/{customerId}/history    customer history
GET    /api/order/business/{businessId}/history    business history
GET    /api/order/courier/{courierId}/history      courier history
GET    /api/order/business/{businessId}/revenue-by-dish?from=&to=
POST   /api/order/create                           create order (idempotent)
POST   /api/order/create/batch                     create many orders (idempotent)
PATCH  /api/order/status?orderId=&status=          transition status
PATCH  /api/order/cancel?orderId=                  cancel
POST   /api/order/courier/deliver                  mark delivered
PATCH  /api/order/courier/mark-paid                CoD settlement
POST   /api/orders/{orderId}/tracking-token        issue tracking-token JWT
```

### Gateway → PaymentService

```
GET    /api/payments/{orderId}                     payment by order id
POST   /api/payments/{paymentId}/cancel            cancel payment
POST   /api/payments/{paymentId}/refunds           full or partial refund
POST   /api/payments/{paymentId}/cash/collect      CoD inkassation
GET    /api/payments/{paymentId}/history           payment history incl. refunds
GET    /api/courier-payouts/{courierId}/balance    pending + available balance
PUT    /api/courier-payouts/{courierId}/account    link courier Stripe account
POST   /webhooks/stripe/main                       payment events webhook
```

### Gateway → TrackingService

```
GET    /api/location?skip=&take=                   list addresses
GET    /api/location/{id}                          address detail
POST   /api/location                               create address
PUT    /api/location/{id}                          update address
DELETE /api/location/{id}                          delete address
POST   /api/location/add                           bulk add
GET    /api/businesslocation/{id}                  business location detail
GET    /api/businesslocation/business/{businessId} business's locations
POST   /api/businesslocation                       create business location
DELETE /api/businesslocation/{id}                  delete business location
```

### Real-time hub

```
WSS    /hubs/courier-tracking                      SignalR hub (CourierTrackingHub)
       methods: SubscribeToUserOrders, UnsubscribeFromUserOrders,
                SubscribeToOrder(orderId), UnsubscribeFromOrder(orderId),
                SendLocation(dto), UpdateTrackingStage(orderId, stage)
       events:  TrackingSnapshotUpdated, CourierLocationUpdated,
                OrderStatusUpdated
```

---

## Frontend

`frontend/food-delivery-platform/`

* **Stack**: React 19, Vite 7, React Router 7, plain CSS modules.
* **Language**: JSX (pages/components) + TypeScript (API clients & models).
* **State**: `UserContext` (auth/profile), per-feature local state, `localStorage` for the access token.
* **HTTP**: axios instance in `src/api/apiClient.ts`
  * `baseURL = VITE_GATEWAY_API_URL ?? "http://localhost:5229/api"`.
  * `withCredentials: true` so the HttpOnly `refreshToken` cookie travels.
  * Request interceptor attaches `Authorization: Bearer <token>`.
  * Response interceptor unwraps the standard `{ success, data, errorMassage }` envelope and pipes business errors through a toast service unless `skipErrorToast` is set.
* **Real-time**: `@microsoft/signalr` client subscribes to the `CourierTrackingHub` for snapshot updates, courier locations, and order-status broadcasts.
* **Maps**: `leaflet` + `react-leaflet` for delivery addresses and live courier markers.
* **Charts**: `recharts` for the business dashboard.
* **Animation**: `framer-motion`.
* **Stripe**: `@stripe/stripe-js` + `@stripe/react-stripe-js` for embedded payment intents and Connect onboarding return/refresh flows.

### Routes (`src/AppRouter.jsx`)

Public:
* `/` — Home
* `/auth` — login/register
* `/restaurants`, `/restaurant/:id`, `/dish/:id`
* `/cart`, `/checkout`
* `/customer/orders`, `/customer/orders/history`
* `/business/orders`, `/business/orders/history`
* `/stripe/onboarding/done`, `/stripe/onboarding/refresh`

Protected (`<ProtectedRoute>`):
* `/profile`, `/account/create`, `/dashboard`
* `/business/dashboard` — revenue by dish, payouts
* `/courier/orders`, `/courier/orders/history`

### Feature folders

* `pages/` — page-level components matching the route table above.
* `features/order-tracking/` — SignalR hooks and live-tracking widgets.
* `global-components/` — Header, Toast, layout primitives.
* `api/` — typed clients: `Account.ts`, `Auth.ts`, `BusinessDashboard.ts`, `BusinessLocation.ts`, `Dish.ts`, `Location.ts`, `Order.ts`, `Payment.jsx`, `Profile.ts`, `User.ts`.
* `context/UserContext.jsx`
* `models/` — response/request type definitions mirroring `DF.Contracts`.

### Build & deploy

* `npm run dev` — local Vite dev server (port 5173).
* `npm run build` — Vite production bundle into `dist/`.
* Production deploy targets **Vercel** (`vercel.json` rewrites everything to `/index.html` for SPA routing).
* The CI also publishes to a `gh-pages` branch via `peaceiris/actions-gh-pages` as a fallback.

---

## API Gateway

`backend/Gateway/DF.Gateway.API/`

The Gateway is **not** Ocelot or YARP — it is a hand-rolled reverse proxy purpose-built for this stack. Responsibilities:

1. **Single client entry point.** The SPA only ever talks to the Gateway.
2. **JWT authentication** of the caller (cookie OR `Authorization` header).
3. **Translation to HMAC-signed internal calls** so downstream services know the request is genuinely from the Gateway and can extract the user identity.
4. **Polly retry** with exponential back-off for idempotent verbs on transient 5xx/network errors.
5. **CORS** — origin whitelisted from `AllowedOrigins:Url`.
6. **Unified error envelope** — every proxied response is wrapped in `Response<T>` (`success`, `data`, `errorMessage`).

### Key components

| File | Role |
| --- | --- |
| `Program.cs` | DI, JWT bearer validation (claim sources: `sub`, `role`, `account_id`, `account_type`), HttpClient + Polly policy, CORS |
| `Attributes/GatewayServiceAttribute.cs` | Decorate each gateway controller with the target `ServiceType` enum |
| `Helpers/ServiceResolver.cs` | Reads the attribute off the endpoint and resolves the downstream base URL from `Services:<Name>` config |
| `Helpers/InternalAuthSigner.cs` | Generates `X-Internal-Timestamp`, `X-Internal-Nonce`, `X-Internal-Signature`, `X-Internal-Key` for every outbound call |
| `Infrastructure/GatewayProxy.cs` | Copies method, body, and non-blocked headers, attaches user-context headers (`X-Internal-UserId/Role/AccountId/AccountType`), forwards the request, deserializes into `Response<T>` |
| `Infrastructure/InternalGatewayContext.cs` | Builds the per-request HMAC bundle |
| `Constants/InternalAuthConstants.cs` | Header names |
| `Middlewares/ExceptionHandlingMiddleware.cs` | Maps unhandled exceptions to the standard error envelope |

### Gateway controller layout

Controllers are organized by downstream service:

```
Controllers/
├── MenuService/    DishController
├── OrderService/   OrderController, OrderTrackingController
├── PaymentService/ PaymentsController, CourierPayoutsController
├── TrackingService/BusinessLocationController, LocationController
└── UserService/    AccountController, AuthController, ProfileController, UserController
```

Each method is a one-liner — `proxy.ProxyAsync<TResponse>(HttpContext)` — because the proxy reads the method, path, query, and body straight from `HttpContext`. The result is that the Gateway's route table is a 1:1 documented surface of every public endpoint.

---

## Shared Contracts (DF.Contracts)

`backend/Contracts/DF.Contracts/` — published as a NuGet package to **GitHub Packages** (`ghcr.io / nuget.pkg.github.com`) and consumed by every service.

Subfolders:

| Folder | Contents |
| --- | --- |
| `Enums/` | `AccountType`, `Category`, `DeliveryMethod`, `OrderStatus`, `PaymentMethod` |
| `EventDriven/` | `AccountCreatedEvent`, `LocationCreatedEvent`, `OrderCreatedEvent`, `OrderStatusChangedEvent`, `OrderPickedUpEvent`, `OrderDeliveredEvent`, `OrderCancelledEvent`, `OrderCourierPaidEvent`, `CourierPayoutCompletedEvent` |
| `Gateway/Requests/` | DTOs accepted at the edge — Accounts, Auth (Login/Register), Dish, Order, Tracking |
| `Gateway/Responses/` | `Response<T>`, `AccountResponse`, `TokenResponse`, `ServiceErrorResponse`, plus per-domain bundles |
| `RPC/Requests/` & `RPC/Responses/` | Per-service RPC envelopes: `UserService`, `MenuService`, `TrackingService` |

The contracts project is built and pushed to GHCR Packages so cross-service references never break on rename.

---

## Microservices

### UserService

`backend/UserService/`

**Domain:**
* `User` (ASP.NET Identity `IdentityUser<Guid>`).
* `Account` (base) with discriminator → `CustomerAccount`, `BusinessAccount`, `CourierAccount`.
* `RefreshToken`, `PayoutRecord`, `ProcessedWebhook`.
* `UserRole`, `AccountType`.

**Responsibilities:**
* Registration, login, logout, refresh — JWT issued here (`TokenService`).
* Account management (customer / business / courier profiles).
* Profile editing + avatar upload via **Cloudinary**.
* **Stripe Connect onboarding** for businesses & couriers: the background `StripeAccountProvisioningWorker` walks pending accounts, calls `StripeConnectService` to create Connect accounts and account links, and persists the IDs.
* Two Stripe webhook controllers: `StripeConnectWebhookController` (account.updated) and `StripePayoutWebhookController` (payout/balance events).
* Webhook idempotency via the `ProcessedWebhook` table + `ProcessedWebhookStore`.

**Auth flow:**
* `POST /api/auth/register|login` → returns `TokenResponse { accessToken, refreshToken, accessTokenExpiresAt }`; refresh token also set as **HttpOnly** cookie.
* `POST /api/auth/refresh` → rotates both tokens.
* `POST /api/auth/logout` → revokes refresh token, clears cookie.
* `/api/auth` and `/health` and `/webhooks` bypass the internal-auth middleware.

**Persistence:** PostgreSQL via EF Core, EF migrations applied on startup. `--migrate` CLI flag for migrator-mode containers.

**Messaging:**
* **Consumes (RPC)** account lookups: `GetAccount`, `GetBusinessAccount`, `GetCustomerAccount`, `GetCourierAccount`, plus batch variants. Implemented in `Application/Messaging/Consumers/` with `RpcConsumerBase`.
* **Calls (RPC)** TrackingService through `TrackingServiceRpcClient` (e.g. to read business locations during account creation).
* **Publishes events** like `AccountCreatedEvent`, `CourierPayoutCompletedEvent`.

**Tests:** `DF.UserService.Tests` (xUnit + Moq + FluentAssertions + EF InMemory) — the unit-test work that this branch (`feature/add-unit-tests-to-user-service`) is focused on.

---

### MenuService

`backend/MenuService/`

**Domain:** `Category`, `Dish`, `Ingredient`, `IdempotencyKey`.

**Responsibilities:**
* CRUD for categories, dishes, and ingredients.
* Dish image upload via **Cloudinary**.
* Aggregating dish data with restaurant info via UserService RPC.

**Validation:** **FluentValidation** wired through `SharpGrip.FluentValidation.AutoValidation.Mvc` so validators in `Application/Validation/` run automatically on incoming MVC requests.

**Caching:** **Redis** (`StackExchange.Redis`, Upstash-friendly connection string) for hot dish lookups.

**Persistence:** PostgreSQL via EF Core, migrations on startup.

**Messaging:**
* **Consumes (RPC)** dish lookups: `GetDishes`, `GetDish`, `GetDishesBatch`.
* **Calls (RPC)** UserService via `UserServiceRpcClient` to enrich dish responses with business info.

---

### OrderService

`backend/OrderService/`

**Domain:** `Order`, `OrderedDish`, `OrderStatus`, `DeliveryMethod`, `PaymentMethod`, `IdempotencyKey`, `OutboxMessage`.

**Responsibilities:**
* End-to-end order lifecycle: Pending → Accepted → Ready → PickedUp → Delivered (or Cancelled at any active step).
* Per-account queries: customer's active list, business open queue, courier's available + active orders, full history per role.
* **Business revenue analytics** — `GET /business/{id}/revenue-by-dish` with optional date range, used by the BusinessDashboard.
* Issues short-lived **tracking-access tokens** (separate JWT) scoped to a single `order_id`, which the SignalR hub validates.

**Reliability:**
* **Idempotency** via `[Idempotent]` filter on creation endpoints + `IdempotencyKey` table.
* **Outbox pattern**: order events written to `OutboxMessage` in the same DB transaction; a `BackgroundJobs` publisher polls and ships them to RabbitMQ.
* **FluentValidation** auto-validation.

**Messaging:**
* **Publishes:** `OrderCreatedEvent`, `OrderStatusChangedEvent`, `OrderPickedUpEvent`, `OrderDeliveredEvent`, `OrderCancelledEvent`, `OrderCourierPaidEvent`.
* **Consumes:** Stripe payment success messages (to mark prepaid orders), tracking notifications.

---

### PaymentService

`backend/PaymentService/`

**Domain:** `Payment`, `PaymentStatus`, `PaymentMethod`, `PaymentTask`, `RefundRecord`, `CourierBalance`, `CourierEarning`, `CourierEarningStatus`, `CourierPayout`, `CourierPayoutStatus`, `FundsFlow`, `Money`, `OutboxMessage`, `OutboxDeadMessage`, `ProcessedMessage`, `ProcessedWebhook`.

**Architecture:** lightweight **CQRS** — explicit `Commands/` records and matching `CommandHandlers/`:

* `CreatePaymentCommand` / Handler
* `CreateStripePaymentIntentCommand` / Handler
* `CollectCashCommand` / Handler
* `CancelPaymentCommand` / Handler
* `RefundPaymentCommand` / Handler

**Stripe integration:**
* `StripeService` wraps the Stripe .NET SDK.
* `StripeOptions` bound from `Stripe:*` config (secret key, webhook secret, default country, dashboard URLs).
* `StripeWebhookController` handles `payment_intent.succeeded`, `charge.refunded`, etc. Webhook idempotency via `ProcessedWebhookStore`.
* `StripeTaskProcessor` (hosted service) drives any work that must hit Stripe out-of-band — capturing intents, syncing PIs — with bounded retries and a 15-min expiration window.

**Courier payouts:**
* `CourierBalance`, `CourierEarning`, `CourierPayout` model the ledger.
* `CourierPayoutWorker` (hosted service) periodically aggregates earnings and creates Stripe transfers to the courier's Connect account.
* `CourierPayoutCompletedEvent` is published; OrderService and UserService consume it.
* HTTP-facing `CourierPayoutsController` exposes status reads.

**Background workers:**
* `OutboxPublisher` — drains the outbox to RabbitMQ (`df.events` exchange) with retry + DLQ promotion via `OutboxDeadMessage`.
* `PaymentTimeoutWorker` — cancels payments stuck past their TTL.
* `StripeTaskProcessor` — async Stripe operations.
* `CourierPayoutWorker` — payout creation.
* `OrderCreatedConsumer`, `OrderCancelledConsumer`, `OrderDeliveredConsumer`, `PaymentSucceededConsumer` — event-driven inbound.

**Resilience:** RabbitMQ connection wrapped in Polly retry; `RabbitMQEventBus` runs with `prefetchCount: 32`, `maxRetries: 3` before dead-lettering.

---

### TrackingService

`backend/TrackingService/`

**Domain:** `Location`, `BusinessLocation`.

**Responsibilities:**
* CRUD for business locations (restaurant addresses), with address normalization.
* Stores a per-order `OrderTrackingSnapshot` (last courier location, stage, status) in Redis.
* Real-time fan-out over **SignalR**.
* Listens to `OrderStatusChangedEvent` from OrderService and pushes to all relevant client groups.

**Persistence:** PostGIS (`postgis/postgis:16-3.4`) for spatial queries. Redis caches active snapshots.

**Real-time:** `Hubs/CourierTrackingHub` (see [Real-time](#real-time-signalr)).

**Auth:** the hub accepts **two** JWT schemes:
* `Bearer` — the user's main JWT (claims: `sub`, `role`, `account_id`, `account_type`).
* `TrackingHub` — the short-lived per-order tracking token issued by OrderService.

**Rate limiting:** ASP.NET rate-limiting middleware applied to hub upgrades and HTTP endpoints (sliding window).

---

### PromoService

`backend/PromoService/DF.PromoService/` — scaffolded module reserved for promotions/coupons; not yet wired into the Gateway or compose stack.

---

## Service-to-service communication

### Async events (`df.events` topic exchange)

| Event | Producer | Primary consumers |
| --- | --- | --- |
| `OrderCreatedEvent` | OrderService | PaymentService (provision payment), TrackingService (init snapshot) |
| `OrderStatusChangedEvent` | OrderService | TrackingService (broadcast to client groups) |
| `OrderPickedUpEvent` / `OrderDeliveredEvent` / `OrderCancelledEvent` | OrderService | PaymentService (capture/refund/settlement) |
| `OrderCourierPaidEvent` | OrderService | PaymentService (cash-on-delivery reconciliation) |
| `AccountCreatedEvent` | UserService | (extensible) |
| `LocationCreatedEvent` | TrackingService | UserService (link to business profile) |
| `CourierPayoutCompletedEvent` | PaymentService | UserService (update PayoutRecord), OrderService |

Reliability: the **transactional outbox** pattern is used by OrderService and PaymentService — events are written to an `OutboxMessage` row in the same transaction as the state change, then a hosted service drains them to RabbitMQ with exponential back-off and a poison-message dead-letter table.

### RPC over RabbitMQ (direct reply-to)

For synchronous lookups across services without coupling to HTTP base URLs:

* **UserService consumers:** `GetAccount`, `GetBusinessAccount`, `GetCustomerAccount`, `GetCourierAccount`, plus batch variants.
* **MenuService consumers:** `GetDish`, `GetDishes`, `GetDishesBatch`.
* **Clients:** `UserServiceRpcClient`, `TrackingServiceRpcClient`, etc., wired as singletons in `Program.cs`.

`RpcConsumerBase` and the shared `MessagingTopology` declare queues idempotently with consistent naming.

---

## Real-time (SignalR)

`backend/TrackingService/DF.TrackingService.API/Hubs/CourierTrackingHub.cs`

Methods exposed to clients:

| Method | Allowed callers | Effect |
| --- | --- | --- |
| `SubscribeToUserOrders()` | Any authenticated user | Joins `customer:{id}` / `business:{id}` / `courier:{id}` based on JWT claims. Couriers additionally join `couriers:available`. |
| `UnsubscribeFromUserOrders()` | Same as above | Leaves those groups. |
| `SubscribeToOrder(orderId)` | Holder of an order-scoped tracking token | Joins `order:{orderId}`, receives the initial snapshot. |
| `UnsubscribeFromOrder(orderId)` | Same | Leaves the order group. |
| `SendLocation(CourierLocationDto)` | Courier with `tracking:write` scope and matching `order_id` claim | Updates the snapshot, broadcasts `TrackingSnapshotUpdated` + `CourierLocationUpdated`. |
| `UpdateTrackingStage(orderId, stage)` | Same | Validates stage transitions (e.g. `ToCustomer` requires business-confirmed pickup), persists, broadcasts. |

Server → client messages: `TrackingSnapshotUpdated`, `CourierLocationUpdated`, `OrderStatusUpdated`.

---

## Authentication & internal security

### Client → Gateway

* **JWT bearer** issued by UserService (`TokenService`) on login/register.
* Configured claims: `sub`, `role`, `account_id`, `account_type`, plus standard JWT fields.
* Access token TTL: 60–120 minutes (configurable).
* Refresh token TTL: 30 days; persisted as `RefreshToken` row, delivered as **HttpOnly cookie**.
* JWT validation lives in the Gateway (`JwtBearerOptions` with `ValidateIssuer/Audience/SigningKey/Lifetime` + 30s clock skew).
* The Gateway accepts the token from either the `Authorization` header or the `accessToken` cookie.

### Gateway → microservice (HMAC signing)

Every proxied request includes four headers added by `InternalAuthSigner`:

| Header | Purpose |
| --- | --- |
| `X-Internal-Timestamp` | Unix seconds; rejected if older than 5 minutes |
| `X-Internal-Nonce` | Per-request random value (replay defense pairs with timestamp) |
| `X-Internal-Signature` | HMAC over `timestamp + nonce` with the shared secret |
| `X-Internal-Key` | Shared API key; compared with `CryptographicOperations.FixedTimeEquals` to defeat timing attacks |

Plus user-context headers extracted from the verified JWT:

* `X-Internal-UserId`, `X-Internal-Role`, `X-Internal-AccountId`, `X-Internal-AccountType`.

`InternalAuthMiddleware` in each downstream service rejects (401) any request without valid signature/key/timestamp. `UserContextMiddleware` then materializes `IUserContext` from the user-context headers and exposes it via DI.

Endpoints that are intentionally exempt from internal auth: `/api/auth/*`, `/health*`, `/webhooks/*` (Stripe webhooks have their own signature verification).

---

## Payments & payouts (Stripe)

The platform uses **Stripe Connect (Express)** so each business and courier has their own connected account, with the platform taking application fees and routing payouts directly.

* **Onboarding** (UserService): `StripeConnectService` creates a Connect account, then an account link with `return_url` and `refresh_url` pointing at the SPA's `/stripe/onboarding/done` and `/stripe/onboarding/refresh` pages. The background `StripeAccountProvisioningWorker` handles retries.
* **Charging** (PaymentService): `CreateStripePaymentIntentCommandHandler` creates a PaymentIntent with `application_fee_amount` and `transfer_data.destination = business.stripeAccountId`. The SPA confirms it via Stripe.js Elements.
* **Refunds**: `RefundPaymentCommandHandler` supports full or partial.
* **Cash on delivery**: `CollectCashCommandHandler` + the courier-side `mark-paid` endpoint in OrderService.
* **Courier payouts**: `CourierPayoutWorker` periodically aggregates `CourierEarning` rows into `CourierPayout` records and issues Stripe transfers to the courier's connected account.
* **Webhooks**:
  * `StripeConnectWebhookController` (UserService) for `account.updated`, etc.
  * `StripePayoutWebhookController` (UserService) for payout/balance events.
  * `StripeWebhookController` (PaymentService) for payment & refund events.
  * All three use shared-secret signature validation and the `ProcessedWebhook` table for idempotency.

---

## Persistence

| Service | Engine | Notes |
| --- | --- | --- |
| UserService | PostgreSQL 16 | EF Core + ASP.NET Identity stores |
| MenuService | PostgreSQL 16 | EF Core; Redis cache layer |
| OrderService | PostgreSQL 16 | EF Core; outbox & idempotency tables |
| PaymentService | PostgreSQL 16 | EF Core; outbox + dead-letter + processed-message tables |
| TrackingService | PostGIS 16-3.4 | spatial types for addresses; Redis for snapshots |
| Shared cache | Redis 7.2 | MenuService + TrackingService |
| Broker | RabbitMQ 3 (with management UI on :15672) | events + RPC |

Every service runs migrations on startup (`db.Database.MigrateAsync()`), and additionally supports a `--migrate` CLI mode used by the migrator containers in `compose.yaml` so the API containers don't race on the same DDL.

---

## Observability

Each backend service is configured for **OpenTelemetry**:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = ...))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = ...));
```

OTLP endpoint is taken from `OTEL_EXPORTER_OTLP_ENDPOINT` (config or env) — point it at any OTLP-compatible collector (Tempo, Jaeger, Honeycomb, Grafana Cloud, …).

**Logging:** Serilog with `CompactJsonFormatter`, enriched with `service`, `traceId`, `requestId`. `UseSerilogRequestLogging()` adds per-request structured log lines.

**Health checks:** every service exposes three endpoints:
* `/health` — liveness (process-only, always 200 if the host is up). Used by Render's default probe.
* `/health/live` — same as `/health`, explicit.
* `/health/ready` — Postgres + RabbitMQ checks. Used by orchestrators for readiness gating.

---

## Resilience & reliability patterns

* **Polly retry** on Gateway → service for idempotent verbs (GET/HEAD/OPTIONS), exponential back-off, 3 attempts.
* **Polly resilience pipeline** wrapping RabbitMQ connection creation in every service (8 retries, exponential, jittered).
* **RabbitMQ auto-recovery**: `AutomaticRecoveryEnabled`, `TopologyRecoveryEnabled`, 30s heartbeat.
* **Transactional outbox** in OrderService and PaymentService.
* **Dead-letter** promotion in PaymentService (`OutboxDeadMessage`) after `MaxRetries: 5` with exponential delay capped at 60s.
* **Idempotency keys** on order creation, payment commands, and webhook delivery (`ProcessedWebhook`, `ProcessedMessage`).
* **Timing-safe API key comparison** in `InternalAuthMiddleware`.
* **Replay defense** via 5-minute timestamp window on every internal call.
* **Rate limiting** on TrackingService hub upgrades.

---

## Testing

* `backend/UserService/DF.UserService.Tests/`
  * xUnit 2.9, Moq 4.20, FluentAssertions 6.12, EF Core InMemory 10.0.
  * Subfolders: `Services/` (unit tests for `AuthService`, `UserService`, `AccountService`, etc.) and `Helpers/`.
  * `GlobalUsings.cs` for terse test files.
  * Active work on this branch (`feature/add-unit-tests-to-user-service`) is expanding coverage.
* Other services run `dotnet test <Service>.sln` in CI even when the test project is currently empty — the structure is wired so adding tests requires no pipeline changes.

---

## CI/CD

`.github/workflows/` — one workflow per deployable unit (the **path filter** on `push` triggers only the relevant pipeline).

### Backend services & Gateway (`userservice.yml`, `menuservice.yml`, `orderservice.yml`, `paymentservice.yml`, `trackingservice.yml`, `gateway.yml`)

All share the same three-job shape:

1. **ci**
   * Checkout, `setup-dotnet@v4` (`DOTNET_VERSION: 10.0.x`).
   * Cache `~/.nuget/packages` keyed on `**/*.csproj`.
   * **Authenticate to GitHub Packages NuGet feed** (the `DF.Contracts` package lives there).
   * `dotnet restore` → `dotnet build -c Release --no-restore` → `dotnet test -c Release --no-build`.

2. **docker** (`needs: ci`)
   * `docker/setup-buildx-action`, login to **GHCR**.
   * `docker/build-push-action` against the service's `Dockerfile`.
   * Tags: `ghcr.io/rostyslav-bodnar/<service>:latest` and `:<sha>`.
   * `cache-from: type=gha` / `cache-to: type=gha,mode=max`.
   * `push` is gated on `github.ref == 'refs/heads/main'` — PRs only build.

3. **deploy** (`needs: docker`, main only)
   * `curl -X POST $RENDER_DEPLOY_HOOK` against the per-service hook secret (e.g. `USERSERVICE_RENDER_DEPLOY_HOOK`, `GATEWAY_RENDER_DEPLOY_HOOK`).
   * Render pulls the freshly pushed `:latest` from GHCR.

Concurrency groups (`group: <service>-${{ github.ref }}, cancel-in-progress: true`) cancel superseded runs on the same branch.

### Frontend (`frontend.yml`)

* `setup-node@v4` with `NODE_VERSION: 22`, npm cache.
* `npm ci` → `npm run build`.
* Uploads `dist/` as an artifact (7-day retention).
* On `main`, downloads the artifact and **deploys to GitHub Pages via `peaceiris/actions-gh-pages@v4`**, publishing to the `gh-pages` branch with `GITHUB_TOKEN` (no PAT required).
* Production is also served from Vercel; the `gh-pages` branch is a backup deployment target.

### Required secrets

* `USERSERVICE_RENDER_DEPLOY_HOOK`, `MENUSERVICE_RENDER_DEPLOY_HOOK`, `ORDERSERVICE_RENDER_DEPLOY_HOOK`, `PAYMENTSERVICE_RENDER_DEPLOY_HOOK`, `TRACKINGSERVICE_RENDER_DEPLOY_HOOK`, `GATEWAY_RENDER_DEPLOY_HOOK`.
* `GITHUB_TOKEN` (default) is used for GHCR push, NuGet pull, and `gh-pages` push.

---

## Deployment topology

| Component | Host |
| --- | --- |
| Frontend SPA | **Vercel** (`food-delivery-platform-eosin.vercel.app`); GitHub Pages mirror |
| Gateway | **Render** (`gateway-5qzd.onrender.com`) |
| Each microservice | **Render** Docker service from GHCR `:latest` |
| Postgres × 5 | Managed Postgres (one DB per service) |
| Redis | Upstash (TLS, `host:port,password=...,ssl=true,abortConnect=false` connection string) |
| RabbitMQ | Cloud broker (CloudAMQP/RabbitMQ Cloud) |
| Object storage | **Cloudinary** for dish & profile images |
| Payments | **Stripe Connect (Express)** |

Each microservice's CORS origin and DB connection string are overridden on Render via environment variables (`AllowedOrigins__Url`, `ConnectionStrings__<Name>`, `RabbitMQ__Url`, `Redis__ConnectionString`, `Stripe__*`, `Cloudinary__*`, `Internal__ApiKey`, `Jwt__*`, `OTEL_EXPORTER_OTLP_ENDPOINT`).

---

## Local development

### Prerequisites

* **.NET 10 SDK** (pinned in `backend/global.json` to `10.0.300`).
* **Node.js 22**.
* **Docker** (Desktop / Engine + Compose v2).
* `docker network create df-network` once.

### Option A — full Docker stack

```bash
docker network create df-network        # one-time
docker compose up -d --build
# Frontend runs on the host:
cd frontend/food-delivery-platform
npm install
npm run dev   # Vite serves at http://localhost:5173; hits Gateway at :5229
```

Inside containers `ASPNETCORE_ENVIRONMENT=Production` is set so `appsettings.Development.json` (which targets `localhost`) is **not** loaded — services talk via the `df-network` DNS names baked into `appsettings.json`.

### Option B — hybrid (infra in Docker, services on the host)

```bash
docker network create df-network
docker compose -f compose.dev-infra.yaml up -d
# Then run each .sln from Rider / VS / dotnet run.
cd frontend/food-delivery-platform && npm run dev
```

`compose.dev-infra.yaml` brings up only Postgres × 5, RabbitMQ, and Redis. Each microservice's `appsettings.Development.json` and `launchSettings.json` point at `localhost:<host-port>` (5433-5437, 5672/15672, 6379).

### Default host ports

| Service | Container → host port |
| --- | --- |
| UserService API | 10000 → 5000 |
| PaymentService API | 8080 → 5003 |
| MenuService API | 8080 → 5004 |
| OrderService API | 8080 → 5005 |
| TrackingService API | 8080 → 5006 |
| Gateway API | 8080 → 5229 |
| UserService DB | 5432 → 5433 |
| MenuService DB | 5432 → 5434 |
| OrderService DB | 5432 → 5435 |
| PaymentService DB | 5432 → 5436 |
| TrackingService DB (PostGIS) | 5432 → 5437 |
| RabbitMQ AMQP / UI | 5672, 15672 |
| Redis | 6379 |

---

## Environment & configuration

All services follow the same convention: `appsettings.json` is the production-shaped skeleton (Docker DNS names, empty secrets), and `appsettings.Development.json` overrides for local host runs. Production overrides come from environment variables on Render.

Common config keys:

| Key | Used by | Purpose |
| --- | --- | --- |
| `ConnectionStrings:<Service>Database` | each service | Postgres connection string |
| `RabbitMQ:Url` | all services | `amqp://user:pass@host:5672/vhost` |
| `Redis:ConnectionString` / `Redis:InstanceName` | MenuService, TrackingService | StackExchange.Redis CS |
| `Jwt:Key` / `Issuer` / `Audience` / `AccessTokenExpirationMinutes` / `RefreshTokenExpirationDays` | UserService, Gateway | JWT signing & validation |
| `Internal:ApiKey` | Gateway + every downstream service | HMAC shared secret |
| `AllowedOrigins:Url` | every service | CORS origin (Gateway URL for services, SPA URL for Gateway) |
| `Cloudinary:CloudName` / `ApiKey` / `ApiSecret` / `Url` | UserService, MenuService | image storage |
| `Stripe:SecretKey` / `WebhookSecretConnect` / `WebhookSecretPayout` / `DefaultCountry` / `Dashboard:ReturnUrl` / `Dashboard:RefreshUrl` | UserService, PaymentService | Stripe Connect & payments |
| `Services:UserService` / `MenuService` / `OrderService` / `PaymentService` / `TrackingService` | Gateway | downstream base URLs |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | all services | OpenTelemetry collector |
| `CourierPayouts:*` | PaymentService | scheduling for `CourierPayoutWorker` |

Frontend config (Vite):

| Var | Purpose |
| --- | --- |
| `VITE_GATEWAY_API_URL` | Production Gateway base URL (defaults to `http://localhost:5229/api`) |

---

## Third-party integrations

| Integration | Where | What it does |
| --- | --- | --- |
| **Stripe Connect (Express)** | UserService, PaymentService, Frontend | Onboarding, payment intents, refunds, courier transfers, payout/connect webhooks |
| **Cloudinary** | UserService, MenuService | Image upload + delivery for dish & profile avatars |
| **PostGIS** | TrackingService DB | Spatial types & queries for business locations |
| **Leaflet / OpenStreetMap** | Frontend | Map rendering for restaurants, addresses, courier markers |
| **SignalR** | TrackingService ↔ Frontend | Real-time courier GPS, status push |
| **RabbitMQ** | All backend services | Event bus + RPC backbone |
| **Redis (Upstash-compatible)** | MenuService, TrackingService | Hot caches and order-tracking snapshots |
| **OpenTelemetry / OTLP** | All backend services | Tracing + metrics export |
| **Serilog (Compact JSON)** | All backend services | Structured logging |
| **GitHub Packages (NuGet + Container)** | CI/CD | Shared `DF.Contracts` package + per-service Docker images on GHCR |
| **Render** | All backend services | Hosting + deploy hooks triggered by CI |
| **Vercel** | Frontend | SPA hosting with SPA rewrites in `vercel.json` |
| **GitHub Pages** | Frontend (mirror) | Backup deploy from the `gh-pages` branch |

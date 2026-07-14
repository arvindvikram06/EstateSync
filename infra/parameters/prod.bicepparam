using '../main.bicep'

param location = 'southindia'
param prefix = 'estatesync'

// PostgreSQL
param postgresAdminUser = 'esadmin'
param postgresAdminPassword = 'ngoJujqqiAuXDXpM9DDJkQ=='

// Your Azure AD Object ID — get it by running:
// az ad signed-in-user show --query id -o tsv
param deployerObjectId = 'bfbd5fb1-f2bb-4328-abd0-d8a5827936f4'

// JWT secret — generate with: openssl rand -base64 32
param jwtSecretKey = 'dawdsucoperkeyjdimwmtmt123eds'

// Stripe live secret key from https://dashboard.stripe.com/apikeys
param stripeSecretKey = 'sk_test_51TiE0VDUyrtuVJJetwyI5lldAnBbXWMqC1HUoek8A1mIRyxIVcRkkihIQpFEvx83y8S9uMhE6b5LnnSGurnk03Op00lkliBjkM'

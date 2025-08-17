-- Seed data for development environment
-- This file automatically loads when MySQL container starts

USE ExpenseTracker;

-- Insert test users (passwords are hashed with BCrypt for "TestPass123!")
INSERT INTO Users (Id, Email, FirstName, LastName, PasswordHash, PasswordSalt, OAuthId, OAuthProvider, IsEmailVerified, CreatedAt, UpdatedAt) VALUES
(
    'dev-user-1111-1111-111111111111',
    'john.doe@example.com',
    'John',
    'Doe', 
    '$2a$12$LQv3c1yqBWVHxkd0LQ3lsOHcsEH.ChUlBL8/Vh9wYaU5Q9d5bD8p6',  -- TestPass123!
    'dev_salt_12345',
    NULL,
    NULL,
    1,
    NOW(),
    NOW()
),
(
    'dev-user-2222-2222-222222222222',
    'jane.smith@example.com',
    'Jane',
    'Smith',
    '$2a$12$LQv3c1yqBWVHxkd0LQ3lsOHcsEH.ChUlBL8/Vh9wYaU5Q9d5bD8p6',  -- TestPass123!
    'dev_salt_67890',
    NULL,
    NULL,
    1,
    NOW(),
    NOW()
),
(
    'oauth-user-3333-3333-333333333333',
    'google.user@gmail.com',
    'Google',
    'User',
    NULL,
    NULL,
    'google_oauth_123456789',
    'Google',
    1,
    NOW(),
    NOW()
);

-- Insert default categories
INSERT INTO Categories (Id, UserId, Name, Color, CreatedAt, UpdatedAt) VALUES
('cat-1111-1111-111111111111', 'dev-user-1111-1111-111111111111', 'Food & Dining', '#FF6B6B', NOW(), NOW()),
('cat-2222-2222-222222222222', 'dev-user-1111-1111-111111111111', 'Transportation', '#4ECDC4', NOW(), NOW()),
('cat-3333-3333-333333333333', 'dev-user-1111-1111-111111111111', 'Shopping', '#45B7D1', NOW(), NOW()),
('cat-4444-4444-444444444444', 'dev-user-1111-1111-111111111111', 'Entertainment', '#96CEB4', NOW(), NOW()),
('cat-5555-5555-555555555555', 'dev-user-1111-1111-111111111111', 'Bills & Utilities', '#FECA57', NOW(), NOW()),
('cat-6666-6666-666666666666', 'dev-user-2222-2222-222222222222', 'Food & Dining', '#FF6B6B', NOW(), NOW()),
('cat-7777-7777-777777777777', 'dev-user-2222-2222-222222222222', 'Healthcare', '#F8B500', NOW(), NOW());
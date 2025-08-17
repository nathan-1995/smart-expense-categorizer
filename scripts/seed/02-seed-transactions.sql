-- Sample transactions for development testing
USE ExpenseTracker;

-- Insert sample transactions for John Doe
INSERT INTO Transactions (Id, UserId, Amount, Description, Date, CategoryId, IsAutoTagged, ConfidenceScore, CreatedAt, UpdatedAt) VALUES
('txn-1111-1111-111111111111', 'dev-user-1111-1111-111111111111', -25.99, 'Starbucks Coffee', '2025-08-15', 'cat-1111-1111-111111111111', 1, 0.95, NOW(), NOW()),
('txn-2222-2222-222222222222', 'dev-user-1111-1111-111111111111', -45.50, 'Uber Ride', '2025-08-15', 'cat-2222-2222-222222222222', 1, 0.88, NOW(), NOW()),
('txn-3333-3333-333333333333', 'dev-user-1111-1111-111111111111', -120.00, 'Grocery Shopping - Whole Foods', '2025-08-14', 'cat-1111-1111-111111111111', 1, 0.92, NOW(), NOW()),
('txn-4444-4444-444444444444', 'dev-user-1111-1111-111111111111', -15.99, 'Netflix Subscription', '2025-08-13', 'cat-4444-4444-444444444444', 1, 0.99, NOW(), NOW()),
('txn-5555-5555-555555555555', 'dev-user-1111-1111-111111111111', -89.99, 'Electric Bill', '2025-08-12', 'cat-5555-5555-555555555555', 0, NULL, NOW(), NOW()),
('txn-6666-6666-666666666666', 'dev-user-1111-1111-111111111111', 2500.00, 'Salary Deposit', '2025-08-01', NULL, 0, NULL, NOW(), NOW()),
('txn-7777-7777-777777777777', 'dev-user-1111-1111-111111111111', -67.89, 'Gas Station', '2025-08-11', 'cat-2222-2222-222222222222', 1, 0.85, NOW(), NOW()),
('txn-8888-8888-888888888888', 'dev-user-1111-1111-111111111111', -199.99, 'Amazon Purchase', '2025-08-10', 'cat-3333-3333-333333333333', 1, 0.78, NOW(), NOW()),

-- Insert sample transactions for Jane Smith
('txn-9999-9999-999999999999', 'dev-user-2222-2222-222222222222', -32.50, 'Restaurant Lunch', '2025-08-15', 'cat-6666-6666-666666666666', 1, 0.91, NOW(), NOW()),
('txn-aaaa-aaaa-aaaaaaaaaaaa', 'dev-user-2222-2222-222222222222', -150.00, 'Doctor Visit', '2025-08-14', 'cat-7777-7777-777777777777', 0, NULL, NOW(), NOW()),
('txn-bbbb-bbbb-bbbbbbbbbbbb', 'dev-user-2222-2222-222222222222', -75.25, 'Coffee Shop Meeting', '2025-08-13', 'cat-6666-6666-666666666666', 1, 0.89, NOW(), NOW()),
('txn-cccc-cccc-cccccccccccc', 'dev-user-2222-2222-222222222222', 3200.00, 'Freelance Payment', '2025-08-01', NULL, 0, NULL, NOW(), NOW());

-- Insert sample budgets
INSERT INTO Budgets (Id, UserId, CategoryId, Amount, Month, Year, CreatedAt, UpdatedAt) VALUES
('budget-1111-1111-111111111111', 'dev-user-1111-1111-111111111111', 'cat-1111-1111-111111111111', 500.00, 8, 2025, NOW(), NOW()),
('budget-2222-2222-222222222222', 'dev-user-1111-1111-111111111111', 'cat-2222-2222-222222222222', 200.00, 8, 2025, NOW(), NOW()),
('budget-3333-3333-333333333333', 'dev-user-1111-1111-111111111111', 'cat-3333-3333-333333333333', 300.00, 8, 2025, NOW(), NOW()),
('budget-4444-4444-444444444444', 'dev-user-1111-1111-111111111111', 'cat-4444-4444-444444444444', 100.00, 8, 2025, NOW(), NOW()),
('budget-5555-5555-555555555555', 'dev-user-2222-2222-222222222222', 'cat-6666-6666-666666666666', 400.00, 8, 2025, NOW(), NOW());
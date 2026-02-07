# Alpha: User Management (Admin Portal)

## Manual Test Steps

### Create user
1. Log in as an Admin user.
2. Navigate to **Administration → User Management**.
3. Select **Create User**.
4. Enter an email address, choose one or more roles, and enter a temporary password (min 10 chars).
5. Submit and verify a success banner/toast appears.
6. Confirm the new user appears in the list.

### Assign role changes
1. From the User Management list, choose **Edit Roles** for a user.
2. Add or remove roles and save.
3. Verify a success banner/toast appears and the table reflects the updated roles.

### Verify non-admin cannot see the page
1. Log in as a non-admin user (e.g., Dispatcher).
2. Confirm the **User Management** navigation item is not visible.
3. Attempt to navigate directly to `/admin/users` and confirm access is denied or redirected.

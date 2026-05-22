## API Endpoints Used

| Method | Endpoint | Used By |
|--------|----------|---------|
| POST | `/api/auth/register` | Register page |
| POST | `/api/auth/login` | Login page |
| GET | `/api/leaverequest/my leave` | Employee dashboard |
| POST | `/api/leaverequest/apply` | Employee dashboard |
| DELETE | `/api/leaverequest/cancel/{id}` | Employee dashboard |
| GET | `/api/leaverequest/pending` | Manager + HR dashboard |
| POST | `/api/approval/process` | Manager dashboard |
| GET | `/api/approval/history` | HR dashboard |

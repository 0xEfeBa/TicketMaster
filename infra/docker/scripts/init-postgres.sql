-- TicketFlow — servis başına bir veritabanı (plan 3.1)
-- Bu betik yalnızca PostgreSQL veri dizini ilk oluşturulurken çalışır (docker-entrypoint-initdb.d).

CREATE DATABASE ticketflow_identity;
CREATE DATABASE ticketflow_catalog;
CREATE DATABASE ticketflow_booking;

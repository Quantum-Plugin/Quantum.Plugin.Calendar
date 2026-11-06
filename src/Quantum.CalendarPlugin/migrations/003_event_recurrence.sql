ALTER TABLE "OfficialCalendarEntries" ADD COLUMN "RepeatPattern" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "OfficialCalendarEntries" ADD COLUMN "RepeatInterval" INTEGER NOT NULL DEFAULT 1;
ALTER TABLE "OfficialCalendarEntries" ADD COLUMN "RepeatWeekdays" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "OfficialCalendarEntries" ADD COLUMN "RepeatWeekOfMonth" INTEGER NULL;
ALTER TABLE "OfficialCalendarEntries" ADD COLUMN "RepeatUntil" TEXT NULL;
ALTER TABLE "OfficialCalendarEntries" ADD COLUMN "RepeatCount" INTEGER NULL;

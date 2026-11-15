# Simplification notes

This version uses plain page headings and instructions, a flat background, smaller headings and simpler cards. The stylesheet is expanded so it is easier to read. The README explains the app in shorter terms.

No feature, test, database migration or architecture layer was removed. These support the final requirements for functionality, reusable components, state management, testing, documentation and advanced features. The existing priority calculation and scheduler provide advanced functionality without adding accounts or cloud services.

The original presentation and report are separate supplied files and are not updated by this source package. Update their screenshots after running this version. Keep the technology comparison and architecture slides. Replace proposed team roles with actual contributions.

## Before submission

- Fix the backup export/import size mismatch.
- Make scheduling reject elapsed hours today.
- Confirm before deleting a study window with saved sessions.
- Run browser tests and inspect populated pages on desktop and mobile.
- Standardize team-name spelling across slides and documents.
- Update historical planning statements as work is completed.

Those first three behavior changes are not included in this presentation simplification. Earlier QA measurements describe the original version. Current build/test results are recorded separately below.

Making the project easier to read does not establish who wrote it. Both students should make and understand real code contributions.

## Verification on 14 November 2026

Debug build: zero warnings and errors. All 60 .NET tests passed. Git diff whitespace check passed. Browser and visual checks were not rerun because browser launch was blocked in this environment.

## Syllabus correction on 15 November 2026

The course syllabus, page 4, requires explicit instructor permission to use AI tools and treats uncredited or unauthorized AI-generated work as plagiarism. This package does not establish that permission has been granted. Credit permitted assistance accurately; simpler wording or styling does not change these requirements.

The earlier recommendation to remove the AI-policy requirement was incorrect. It has been restored in the README, technical specification and submission checklist. Keep the report and presentation: syllabus pages 9-10 assess both. This correction changes documentation only; the 14 November build/test results above remain the most recent executed checks. The application bugs listed above are still open.

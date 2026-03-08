---
created: 2026-03-08T08:15:35.353Z
title: Filter assign modal coachee list to coachee-role users
area: ui
files:
  - Views/Admin/CoachCoacheeMapping.cshtml
  - Controllers/AdminController.cs
---

## Problem

In the CoachCoacheeMapping assign modal, the coachee dropdown lists all users. User wants it to automatically list only users who have the coachee role, excluding users with other roles that shouldn't appear as assignable coachees.

## Solution

Filter the coachee list query in AdminController to only include users with the appropriate role (e.g., where user has coachee/Level 6 role). Update the ViewBag or model that populates the assign modal's coachee select element.

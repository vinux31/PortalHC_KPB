using System;
using System.Linq.Expressions;
using HcPortal.Models;

namespace HcPortal.Helpers;

public static class SiblingSessionQuery
{
    // WSE-04 (D-01/D-09): type-aware sibling isolation. Hanya PreTest/PostTest yang diisolasi;
    // Standard/""/null dikelompokkan bersama sebagai non-PrePost (zero behavior-change + aman legacy).
    // Catatan D-01: kolom linked-group SENGAJA tidak dipakai sebagai pemisah (Pre & Post berbagi nilai
    // sama). AssessmentType satu-satunya diskriminator. Dipakai IDENTIK di StartExam + ReshufflePackage +
    // ReshuffleAll untuk jaga determinisme workerIndex (Phase 373 invariant).
    public static Expression<Func<AssessmentSession, bool>> SiblingPrePostAwarePredicate(
        string title, string category, DateTime scheduleDate, string? assessmentType)
    {
        bool isPrePost = assessmentType == "PreTest" || assessmentType == "PostTest";
        return s => s.Title == title
                    && s.Category == category
                    && s.Schedule.Date == scheduleDate.Date
                    && ( isPrePost
                         ? s.AssessmentType == assessmentType
                         : (s.AssessmentType != "PreTest" && s.AssessmentType != "PostTest") );
    }
}

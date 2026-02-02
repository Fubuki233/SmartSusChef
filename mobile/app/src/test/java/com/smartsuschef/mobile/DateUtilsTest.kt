package com.smartsuschef.mobile

import com.smartsuschef.mobile.util.DateUtils
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class DateUtilsTest {

    @Test
    fun testFormatDateForDisplaySuccess() { // Replaces the backtick name
        val apiDate = "2026-02-02"
        val result = DateUtils.formatDateForDisplay(apiDate)
        assertEquals("02 Feb 2026", result)
    }

    @Test
    fun testFormatDateForDisplayInvalidHandle() {
        val badDate = "not-a-date"
        val result = DateUtils.formatDateForDisplay(badDate)
        assertEquals("not-a-date", result)
    }

    @Test
    fun testIsTodayCorrect() {
        val todayApiString = DateUtils.getTodayFormatted()
        val isToday = DateUtils.isToday(todayApiString)
        assertTrue("Should return true for today", isToday)
    }
}
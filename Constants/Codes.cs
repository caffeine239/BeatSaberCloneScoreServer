﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum Opcodes : uint
{
    MSG_NULL_ACTION = 0,
    AUTHENTICATION_REQUEST = 1,
    AUTHENTICATION_RESULT_OK = 2,
    AUTHENTICATION_RESULT_FAILED = 3,
    AUTHENTICATION_RESULT_EXPIRED = 4,
    AUTHENTICATION_RESULT_INVALID = 5,
    SETTINGS_WRITE = 6,
    REQUEST_SCORES = 7,
    SCORE_RESPONSE = 8,
    SCORE_SUBMIT = 9,
    SIGNUP_REQUEST = 10,
    SIGNUP_RESPONSE = 11,
}
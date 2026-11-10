using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable, Flags]
public enum DialogueType
{

    None = 0, Statement = 1, Question = 2, Reply = 4, Opinion = 8, Fact = 16, Exclamation = 32, Rambling = 64

}

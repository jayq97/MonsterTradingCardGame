﻿using NUnit.Framework;
using SWEN1.MTCG.GameClasses;

namespace SWEN1.MTCG.Test.GameClasses.Test
{
    public class UserTest
    {
        [Test]
        public void Test_CallUser()
        {
            var user1 = new User(1, "Jay");
            Assert.AreEqual(1, user1.ID);
            Assert.AreEqual("Jay", user1.Username);
        }
    }
}
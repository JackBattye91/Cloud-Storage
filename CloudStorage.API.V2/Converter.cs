﻿using CloudStorage.API.V2.Models;
using CloudStorage.API.V2.Models.DTOs;
using System.Reflection;

namespace CloudStorage.API.V2
{
    public class Converter
    {
        public static UserDTO? Convert(User pUser)
        {
            UserDTO? user = Convert<User, UserDTO>(pUser);

            if (user?.Password != null)
            {
                user!.Password = null;
            }

            return user;
        }

        public static Tout? Convert<Tin, Tout>(Tin pTin)
        {
            Tout? output = (Tout?)Activator.CreateInstance(typeof(Tout));

            if (output != null)
            {
                PropertyInfo[] outPropertyInfo = typeof(Tout).GetProperties();
                PropertyInfo[] inPropertyInfo = typeof(Tin).GetProperties();

                foreach (PropertyInfo outProp in outPropertyInfo)
                {
                    if (!outProp.CanWrite)
                    {
                        continue;
                    }

                    PropertyInfo? inProp = inPropertyInfo.Where(x => x.Name == outProp.Name && x.PropertyType == outProp.PropertyType).FirstOrDefault();

                    if (inProp != null)
                    {
                        object? value = inProp.GetValue(pTin, null);
                        outProp.SetValue(output, value, null);
                    }
                }
            }

            return output;
        }
    }
}

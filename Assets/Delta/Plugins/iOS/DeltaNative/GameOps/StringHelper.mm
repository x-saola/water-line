//
//  StringHelper.m
//  Unity-iPhone
//
//  Created by Nguyen Hoai Phuong on 1/14/20.
//

#import "StringHelper.h"

char* cStringCopy(const char* string)
{
   if (string == NULL)
   {
       return NULL;
   }
   char* res = (char*)malloc(strlen(string) + 1);
   strcpy(res, string);
   return res;
}

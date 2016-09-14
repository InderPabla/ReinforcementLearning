#include "stdafx.h"
#include <iostream>

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include<stdio.h>
#include<winsock2.h>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

using namespace std;

class EasyClientSocket {

	private: 
		char *sendbuf = "this is a test";
		WSADATA wsa;
		SOCKET s;
		struct sockaddr_in server;

	public:
		EasyClientSocket(int port, const char *address) {
			

			printf("\nInitialising Winsock...");
			if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
			{
				printf("Failed. Error Code : %d", WSAGetLastError());
			}

			printf("Initialised.\n");

			//Create a socket
			if ((s = socket(AF_INET, SOCK_STREAM, 0)) == INVALID_SOCKET)
			{
				printf("Could not create socket : %d", WSAGetLastError());
			}

			printf("Socket created.\n");


			server.sin_addr.s_addr = inet_addr(address);
			server.sin_family = AF_INET;
			server.sin_port = htons(port);

			//Connect to remote server
			if (connect(s, (struct sockaddr *)&server, sizeof(server)) < 0)
			{
				puts("connect error");
			}
			puts("Connected");

			/*string outMessage = "tesqererq rrqer";
			//int iResult = send(s, sendbuf, (int)strlen(sendbuf), 0);
			int iResult = send(s, outMessage.c_str(), outMessage.size(), 0);
			if (iResult == SOCKET_ERROR) {
				printf("send failed with error: %d\n", WSAGetLastError());
				
				WSACleanup();
			}

			printf("Bytes Sent: %ld\n", iResult);
			closesocket(s);
			*/

			float testArray[5] = {0.24f,0.979f,0.1234f,-1.298f,-0.6698f};
			int testArrayLen = 5;

			for (int i = 0; i < testArrayLen; i++) {
				testArray[i] = ReverseFloat(testArray[i]);
			}

			
			sendArray(testArray, testArrayLen);

			closesocket(s);
		}

		void sendArray(float array[], int arrayLen) {
			int iResult  = send(s, (char*)array, sizeof(array[0]) * arrayLen,0);

			if (iResult == SOCKET_ERROR) {
				printf("send failed with error: %d\n", WSAGetLastError());
				WSACleanup();
			}
			printf("Bytes Sent: %ld\n", iResult);
		}

		float ReverseFloat(const float inFloat)
		{
			float retVal;
			char *floatToConvert = (char*)& inFloat;
			char *returnFloat = (char*)& retVal;

			// swap the bytes into a temporary buffer
			returnFloat[0] = floatToConvert[3];
			returnFloat[1] = floatToConvert[2];
			returnFloat[2] = floatToConvert[1];
			returnFloat[3] = floatToConvert[0];

			return retVal;
		}
};
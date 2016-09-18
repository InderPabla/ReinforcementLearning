#include "stdafx.h" //standard 
#include <iostream>

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include<stdio.h>
#include<winsock2.h>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

using namespace std;

class EasyClientSocket {

	private: 
		WSADATA wsa; 
		SOCKET s;
		struct sockaddr_in server;

	public:
		EasyClientSocket(int port, const char *address) {

			printf("\nInitialising Winsock...");
			if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
				printf("Failed. Erfror Code : %d", WSAGetLastError());
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
			if (connect(s, (struct sockaddr *)&server, sizeof(server)) < 0) {
				puts("connect error");
			}
			puts("Connected");
		}

		// Test sending an array with 5 float values 
		void TestArraySend() {
			float testArray[5] = { 0.24f,0.979f,0.1234f,-1.298f,-0.6698f };
			int testArrayLen = 5;

			SendArray(testArray, testArrayLen);
		}

		// Test receiveing an array with 5 float values
		void TestArrayReceive() {
			const int count = 5;
			float *rcvArray = ReceiveFloatArray(count);

			for (int i = 0; i < count; i++) {
				printf("%f ", rcvArray[i]);
			}

			delete[] rcvArray; //free heap
		}

		void CloseConnection() {
			closesocket(s);
		}

		// send float array over TCP
		void SendArray(float array[], int arrayLen) {
			int iResult  = send(s, (char*)array, sizeof(array[0]) * arrayLen,0); //send float array after conversion to bytes

			if (iResult == SOCKET_ERROR) { // if error during send print error 
				printf("send failed with error: %d\n", WSAGetLastError());
				WSACleanup(); //clean up win sock
			}
			else
				printf("Bytes Sent: %ld\n", iResult); // print number of bytes sent
		}

		// send string over TCP
		void SendString(string message) {
			int iResult = send(s, message.c_str(), message.size(), 0); //send string message

			if (iResult == SOCKET_ERROR) { // if error during send print error 
				printf("send failed with error: %d\n", WSAGetLastError());
				WSACleanup(); //clean up win sock
			}
			else
				printf("Bytes Sent: %ld\n", iResult); // print number of bytes sent
		}
			
		float* ReceiveFloatArray(int count) {
			float *pos = new float[count];

			char buffer[1000];
			int iResult = recv(s, buffer, count * 4, 0);

			if (iResult == SOCKET_ERROR) {
				printf("send failed with error: %d\n", WSAGetLastError());
				WSACleanup(); //clean up win sock
			}
			else
				printf("Bytes Recv: %ld\n", iResult); // print number of bytes sent

			for (int i = 0; i < count * 4; i += 4) {
				int index = i / 4;
				memcpy(&pos[index], &buffer[i], sizeof pos[index]);    // receive data
			}
			return pos;
				
		}

		void OpenConnection() {
			
		}

			
};
module.exports = {
  preset: "jest-expo",
  setupFilesAfterEnv: ["<rootDir>/src/test/setup.ts"],
  moduleNameMapper: {
    "^@/(.*)$": "<rootDir>/src/$1",
    "^msw/node$": "<rootDir>/node_modules/msw/lib/node/index.js"
  },
  testMatch: ["**/*.test.ts", "**/*.test.tsx"],
  transformIgnorePatterns: [
    "node_modules/(?!((jest-)?react-native|@react-native|expo(nent)?|@expo(nent)?/.*|expo-.*|@expo/.*|@react-navigation/.*|@testing-library/react-native|react-native-safe-area-context)/)"
  ]
};
